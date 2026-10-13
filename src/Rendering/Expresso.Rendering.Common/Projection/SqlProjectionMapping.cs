using Expresso.Core.Filtering;
using Expresso.Core.Projection;

namespace Expresso.Rendering;

/// <summary>Maps public projection fields, ordered internal keys, and collection queries.</summary>
public sealed class SqlProjectionMapping
{
    private readonly List<SqlProjectionField> _fields = new List<SqlProjectionField>();
    private readonly List<SqlProjectionKey> _keys = new List<SqlProjectionKey>();
    private readonly Dictionary<string, (string OutputName, SqlCollectionProjectionMapping Mapping)> _collections =
        new Dictionary<string, (string, SqlCollectionProjectionMapping)>(StringComparer.OrdinalIgnoreCase);
    /// <summary>Public fields in declaration order.</summary>
    public IReadOnlyList<SqlProjectionField> Fields => _fields.AsReadOnly();
    /// <summary>Internal key components in declaration order.</summary>
    public IReadOnlyList<SqlProjectionKey> Keys => _keys.AsReadOnly();
    /// <summary>Equality used for this entity's materialized identities and child-to-parent attachment; structural by default.</summary>
    public IEqualityComparer<CompositeProjectionKey> KeyComparer { get; private set; } = EqualityComparer<CompositeProjectionKey>.Default;
    /// <summary>Collection queries by case-insensitive catalog name.</summary>
    public IReadOnlyDictionary<string, (string OutputName, SqlCollectionProjectionMapping Mapping)> Collections =>
        new System.Collections.ObjectModel.ReadOnlyDictionary<string, (string, SqlCollectionProjectionMapping)>(_collections);

    /// <summary>Maps a public field to an application-supplied column expression and JSON output name.</summary>
    public SqlProjectionMapping Field(string name, string outputName, string columnExpression, Type valueType, bool rawExpression = false)
    {
        RequireName(name);
        RequireOutput(outputName);
        _fields.Add(new SqlProjectionField(name.ToLowerInvariant(), outputName, columnExpression, valueType, rawExpression));
        return this;
    }
    /// <summary>Adds an ordered component of the entity's internal composite identity.</summary>
    public SqlProjectionMapping Key(string keyName, string columnExpression, Type valueType, bool rawExpression = false)
    {
        if (_collections.Count > 0) throw new InvalidOperationException("Declare all identity components before collection correlation.");
        if (_keys.Any(k => string.Equals(k.Name, keyName, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Duplicate projection key: " + keyName);
        _keys.Add(new SqlProjectionKey(keyName, columnExpression, valueType, rawExpression));
        return this;
    }

    /// <summary>Configures database-compatible key equality for grouping, attachment and relationship deduplication.</summary>
    /// <remarks>The comparer must match all ordered key components and the actual database collations; SQL predicates retain original values.</remarks>
    public SqlProjectionMapping UseKeyComparer(IEqualityComparer<CompositeProjectionKey> comparer)
    {
        KeyComparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        return this;
    }
    /// <summary>Maps a requested collection. Correlation must cover every parent key in order.</summary>
    public SqlProjectionMapping Collection(string name, string outputName, SqlCollectionProjectionMapping collection)
    {
        RequireName(name);
        RequireOutput(outputName);
        if (collection is null) throw new ArgumentNullException(nameof(collection));
        if (collection.Items.Contains(this)) throw new ArgumentException("Projection mappings must form a finite, acyclic tree.", nameof(collection));
        if (_keys.Count == 0 || _keys.Count != collection.ParentKeys.Count ||
            _keys.Where((k, i) => !string.Equals(k.Name, collection.ParentKeys[i].Name, StringComparison.OrdinalIgnoreCase) ||
                (Nullable.GetUnderlyingType(k.ValueType) ?? k.ValueType) !=
                (Nullable.GetUnderlyingType(collection.ParentKeys[i].ValueType) ?? collection.ParentKeys[i].ValueType)).Any())
            throw new ArgumentException("Collection correlation must match every parent key component in order.");
        _collections.Add(name.ToLowerInvariant(), (outputName, collection));
        return this;
    }
    /// <summary>Gets an allow-list derived only from public response mappings, independent of filtering.</summary>
    public QueryModel GetProjectionModel() => new QueryModel(Fields.Select(f => (f.Name, f.ValueType)).ToArray(),
        Collections.Select(c => new CollectionModel(c.Key, c.Value.Mapping.Items.GetProjectionModel())).ToArray());

    private bool Contains(SqlProjectionMapping mapping) => ReferenceEquals(this, mapping) || _collections.Values.Any(c => c.Mapping.Items.Contains(mapping));
    private void RequireName(string name)
    {
        SqlProjectionKey.RequireText(name, nameof(name));
        if (_fields.Any(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)) || _collections.ContainsKey(name))
            throw new ArgumentException("Duplicate projection name: " + name);
    }
    private void RequireOutput(string name)
    {
        SqlProjectionKey.RequireText(name, nameof(name));
        if (_fields.Any(f => f.OutputName == name) || _collections.Values.Any(c => c.OutputName == name))
            throw new ArgumentException("Duplicate projection output name: " + name);
    }
}

/// <summary>A mapped public scalar column.</summary>
public sealed class SqlProjectionField : SqlProjectionKey
{
    internal SqlProjectionField(string name, string outputName, string expression, Type type, bool rawExpression) : base(name, expression, type, rawExpression) =>
        OutputName = outputName;
    /// <summary>Response property name.</summary>
    public string OutputName { get; }
}

/// <summary>An ordered scalar identity or parent-correlation component.</summary>
public class SqlProjectionKey
{
    /// <summary>Creates a component from trusted application SQL.</summary>
    public SqlProjectionKey(string name, string columnExpression, Type valueType, bool rawExpression = false)
    {
        RequireText(name, nameof(name));
        RequireText(columnExpression, nameof(columnExpression));
        Name = name.ToLowerInvariant();
        ColumnExpression = columnExpression;
        RawExpression = rawExpression;
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
    }
    /// <summary>Component name.</summary>
    public string Name { get; }
    /// <summary>Trusted column expression.</summary>
    public string ColumnExpression { get; }
    /// <summary>CLR value type.</summary>
    public Type ValueType { get; }
    /// <summary>Whether the trusted expression bypasses identifier quoting.</summary>
    public bool RawExpression { get; }
    internal static void RequireText(string value, string argument)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value must not be empty.", argument);
    }
}

/// <summary>Application-owned child FROM, identity, correlation, and ordering metadata.</summary>
public sealed class SqlCollectionProjectionMapping
{
    /// <summary>Creates a collection query. Parent key names/types must match the parent mapping.</summary>
    public SqlCollectionProjectionMapping(string fromClause, SqlProjectionMapping items,
        IEnumerable<SqlProjectionKey> parentKeys, string? defaultOrderBy = null, SqlQueryMapping? queryMapping = null)
    {
        SqlProjectionKey.RequireText(fromClause, nameof(fromClause));
        Items = items ?? throw new ArgumentNullException(nameof(items));
        if (parentKeys is null) throw new ArgumentNullException(nameof(parentKeys));
        var keys = parentKeys.ToArray();
        if (keys.Length == 0 || keys.Any(k => k is null) ||
            keys.Select(k => k.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != keys.Length)
            throw new ArgumentException("Parent correlation requires unique, non-null key components.", nameof(parentKeys));
        FromClause = fromClause;
        ParentKeys = Array.AsReadOnly(keys);
        DefaultOrderBy = defaultOrderBy;
        QueryMapping = queryMapping;
    }
    /// <summary>Trusted FROM and join text, without the FROM keyword.</summary>
    public string FromClause { get; }
    /// <summary>Item projection mapping.</summary>
    public SqlProjectionMapping Items { get; }
    /// <summary>Child SQL columns matching the parent's ordered key components.</summary>
    public IReadOnlyList<SqlProjectionKey> ParentKeys { get; }
    /// <summary>Trusted stable default ORDER BY text, without its keyword.</summary>
    public string? DefaultOrderBy { get; }
    /// <summary>Existing filter/sort mapping for requested nested ordering.</summary>
    public SqlQueryMapping? QueryMapping { get; }
}
