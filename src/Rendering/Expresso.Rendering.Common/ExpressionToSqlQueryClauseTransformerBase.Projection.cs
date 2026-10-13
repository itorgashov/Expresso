using Expresso.Core.Projection;

namespace Expresso.Rendering;

public abstract partial class ExpressionToSqlQueryClauseTransformerBase : IProjectionToSqlTransformer
{
    /// <inheritdoc />
    public RenderedSqlProjection RenderProjection(ProjectionDirective projection, SqlProjectionMapping mapping)
    {
        if (projection is null) throw new ArgumentNullException(nameof(projection));
        if (mapping is null) throw new ArgumentNullException(nameof(mapping));
        return RenderProjectionCore(projection, mapping, Array.Empty<SqlProjectionKey>());
    }

    private RenderedSqlProjection RenderProjectionCore(ProjectionDirective projection, SqlProjectionMapping mapping,
        IReadOnlyList<SqlProjectionKey> parentKeys)
    {
        if (mapping.Keys.Count == 0)
            throw new ArgumentException("A projection mapping requires at least one entity key.");
        if (mapping.KeyComparer is CompositeProjectionKeyComparer comparer && comparer.ComponentCount != mapping.Keys.Count)
            throw new ArgumentException("Key comparer components must match the mapping's ordered identity.");
        foreach (var selection in projection.Selections)
        {
            var scalar = mapping.Fields.FirstOrDefault(f => string.Equals(f.Name, selection.Name, StringComparison.OrdinalIgnoreCase));
            if (scalar is null && !mapping.Collections.ContainsKey(selection.Name))
                throw new ArgumentException("Unknown projection mapping: " + selection.Name);
            if (scalar is not null && selection.IsCollectionSubtree)
                throw new ArgumentException("Cannot traverse a scalar projection field: " + selection.Name);
        }
        var columns = new List<SqlProjectedColumn>();
        var fields = new List<SqlProjectedField>();
        var children = new List<SqlProjectedCollection>();
        int Add(SqlProjectionKey field)
        {
            var column = columns.FirstOrDefault(c => c.ColumnExpression == field.ColumnExpression && c.ValueType == field.ValueType && c.RawExpression == field.RawExpression);
            if (column is null)
            {
                column = new SqlProjectedColumn(field.ColumnExpression, field.ValueType, columns.Count, field.RawExpression);
                columns.Add(column);
            }
            return column.Ordinal;
        }
        foreach (var field in mapping.Fields.Where(f => projection.IsAll || projection.Find(f.Name) is not null))
            fields.Add(new SqlProjectedField(field.Name, field.OutputName, Add(field)));
        foreach (var pair in mapping.Collections)
        {
            var selected = projection.Find(pair.Key);
            if (!projection.IsAll && selected is null) continue;
            children.Add(new SqlProjectedCollection(pair.Key, pair.Value.OutputName, pair.Value.Mapping,
                RenderProjectionCore(projection.IsAll ? ProjectionDirective.All : selected!.ToDirective(),
                    pair.Value.Mapping.Items, pair.Value.Mapping.ParentKeys)));
        }
        var keys = mapping.Keys.Select(Add).ToArray();
        var parents = parentKeys.Select(Add).ToArray();
        var selectList = string.Join(", ", columns.Select(c =>
            ProjectionExpression(c.ColumnExpression, c.RawExpression) + " AS " + QuoteIdentifierPart(c.Alias)));
        return new RenderedSqlProjection(selectList, columns, fields, keys, parents, children, mapping.KeyComparer);
    }
    private string ProjectionExpression(string expression, bool raw) =>
        !raw && System.Text.RegularExpressions.Regex.IsMatch(expression, @"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)+$")
            ? QuoteIdentifier(expression) : expression;

    /// <inheritdoc />
    public (string whereClause, Dictionary<string, object> parameters) RenderProjectionKeys(
        IReadOnlyList<SqlProjectionKey> components, IReadOnlyList<CompositeProjectionKey> keys, string paramNamePrefix = "key")
    {
        if (components is null) throw new ArgumentNullException(nameof(components));
        if (keys is null) throw new ArgumentNullException(nameof(keys));
        if (paramNamePrefix is null || !PrefixPattern.IsMatch(paramNamePrefix)) throw new ArgumentException("Invalid parameter prefix.", nameof(paramNamePrefix));
        if (components.Count == 0 || keys.Count == 0 || keys.Any(k => !k.IsComplete || k.Count != components.Count))
            throw new ArgumentException("Key predicates require complete keys matching every component.");
        var parameters = new Dictionary<string, object>();
        var predicates = keys.Select(key => "(" + string.Join(" AND ", components.Select((component, index) =>
        {
            var name = FormatParameterName(paramNamePrefix, parameters.Count);
            parameters.Add(name, key[index]!);
            return ProjectionExpression(component.ColumnExpression, component.RawExpression) + " = " + name;
        })) + ")").ToArray();
        return (string.Join(" OR ", predicates), parameters);
    }
}
