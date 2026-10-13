using Expresso.Core.Projection;

namespace Expresso.Rendering;

/// <summary>An immutable select-list plan, including hidden identity and requested child queries.</summary>
public sealed class RenderedSqlProjection
{
    internal RenderedSqlProjection(string selectList, List<SqlProjectedColumn> columns,
        List<SqlProjectedField> fields, int[] keys, int[] parentKeys, List<SqlProjectedCollection> collections,
        IEqualityComparer<CompositeProjectionKey> keyComparer)
    {
        SelectList = selectList;
        Columns = columns.AsReadOnly();
        Fields = fields.AsReadOnly();
        KeyOrdinals = Array.AsReadOnly(keys);
        ParentKeyOrdinals = Array.AsReadOnly(parentKeys);
        Collections = collections.AsReadOnly();
        KeyComparer = keyComparer;
    }
    /// <summary>SELECT expressions without the SELECT keyword.</summary>
    public string SelectList { get; }
    /// <summary>Physical columns in reader order.</summary>
    public IReadOnlyList<SqlProjectedColumn> Columns { get; }
    /// <summary>Visible scalar fields and their physical ordinals.</summary>
    public IReadOnlyList<SqlProjectedField> Fields { get; }
    /// <summary>Ordered internal entity key ordinals.</summary>
    public IReadOnlyList<int> KeyOrdinals { get; }
    /// <summary>Database-compatible equality for materialized identities at this entity level.</summary>
    public IEqualityComparer<CompositeProjectionKey> KeyComparer { get; }
    /// <summary>Ordered parent-correlation ordinals, empty for the root.</summary>
    public IReadOnlyList<int> ParentKeyOrdinals { get; }
    /// <summary>Only the requested collection query plans.</summary>
    public IReadOnlyList<SqlProjectedCollection> Collections { get; }
}

/// <summary>A physical selected column.</summary>
public sealed class SqlProjectedColumn
{
    internal SqlProjectedColumn(string expression, Type type, int ordinal, bool rawExpression)
    {
        RawExpression = rawExpression;
        ColumnExpression = expression;
        ValueType = type;
        Ordinal = ordinal;
        Alias = "c" + ordinal;
    }
    /// <summary>Trusted application column expression.</summary>
    public string ColumnExpression { get; }
    /// <summary>Whether the trusted expression bypasses identifier quoting.</summary>
    public bool RawExpression { get; }
    /// <summary>Expected CLR type.</summary>
    public Type ValueType { get; }
    /// <summary>Zero-based reader ordinal.</summary>
    public int Ordinal { get; }
    /// <summary>Generated collision-free SQL alias.</summary>
    public string Alias { get; }
}

/// <summary>A visible field linked to a selected physical column.</summary>
public sealed class SqlProjectedField
{
    internal SqlProjectedField(string name, string outputName, int ordinal)
    {
        Name = name;
        OutputName = outputName;
        Ordinal = ordinal;
    }
    /// <summary>Query catalog name.</summary>
    public string Name { get; }
    /// <summary>Response property name.</summary>
    public string OutputName { get; }
    /// <summary>Physical reader ordinal.</summary>
    public int Ordinal { get; }
}

/// <summary>A requested collection's execution metadata and recursive select plan.</summary>
public sealed class SqlProjectedCollection
{
    internal SqlProjectedCollection(string name, string outputName, SqlCollectionProjectionMapping mapping, RenderedSqlProjection items)
    {
        Name = name;
        OutputName = outputName;
        Mapping = mapping;
        Items = items;
    }
    /// <summary>Query catalog name.</summary>
    public string Name { get; }
    /// <summary>Response property name.</summary>
    public string OutputName { get; }
    /// <summary>Trusted child SQL metadata.</summary>
    public SqlCollectionProjectionMapping Mapping { get; }
    /// <summary>Child select plan.</summary>
    public RenderedSqlProjection Items { get; }
}
