using Expresso.Core.Projection;
using Expresso.Core.Sorting;

namespace Expresso.Rendering;

/// <summary>Application-owned asynchronous SQL I/O and scalar conversion boundary.</summary>
public interface IProjectionSqlQueryExecutor
{
    /// <summary>Reads rows in plan column order, converting values to each column's CLR type and SQL NULL to null.</summary>
    Task<IReadOnlyList<object?[]>> ReadAsync(string sql, IReadOnlyDictionary<string, object> parameters,
        RenderedSqlProjection plan, CancellationToken cancellationToken);
}

/// <summary>Executes SQL projection plans, batches structural correlations, and shapes sparse responses.</summary>
public sealed class SqlProjectionExecutor
{
    private readonly IProjectionToSqlTransformer _projection;
    private readonly IExpressionToQueryClauseTransformer _clauses;

    /// <summary>Creates an executor with separately registered projection and clause renderers.</summary>
    public SqlProjectionExecutor(IProjectionToSqlTransformer projection, IExpressionToQueryClauseTransformer clauses)
    {
        _projection = projection ?? throw new ArgumentNullException(nameof(projection));
        _clauses = clauses ?? throw new ArgumentNullException(nameof(clauses));
    }

    /// <summary>Runs application-composed root SQL and requested child levels. Connections remain application-owned.</summary>
    /// <param name="plan">Validated select plan.</param>
    /// <param name="rootSql">Complete trusted root SELECT, including filter, ordering, and paging.</param>
    /// <param name="parameters">Root parameters.</param>
    /// <param name="io">Application I/O boundary.</param>
    /// <param name="sort">Nested collection ordering scopes.</param>
    /// <param name="maxKeyParameters">Positive scalar correlation parameter budget; default 500.</param>
    /// <param name="cancellationToken">Cancels all queries.</param>
    /// <returns>Sparse objects without hidden keys.</returns>
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExecuteAsync(RenderedSqlProjection plan,
        string rootSql, IReadOnlyDictionary<string, object> parameters, IProjectionSqlQueryExecutor io,
        SortDirective? sort = null, int maxKeyParameters = 500, CancellationToken cancellationToken = default)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));
        if (string.IsNullOrWhiteSpace(rootSql)) throw new ArgumentException("Root SQL must not be empty.", nameof(rootSql));
        if (parameters is null) throw new ArgumentNullException(nameof(parameters));
        if (io is null) throw new ArgumentNullException(nameof(io));
        if (maxKeyParameters <= 0) throw new ArgumentOutOfRangeException(nameof(maxKeyParameters));
        ValidateBudget(plan, maxKeyParameters);
        var rows = Shape(await io.ReadAsync(rootSql, parameters, plan, cancellationToken).ConfigureAwait(false), plan);
        await ChildrenAsync(rows, plan, io, sort, Array.Empty<string>(), maxKeyParameters, cancellationToken).ConfigureAwait(false);
        return rows.Select(r => (IReadOnlyDictionary<string, object?>)r.Values).ToArray();
    }

    private static void ValidateBudget(RenderedSqlProjection plan, int budget)
    {
        foreach (var child in plan.Collections)
        {
            if (budget < child.Items.ParentKeyOrdinals.Count)
                throw new ArgumentException("Parameter budget is smaller than one composite key.", nameof(budget));
            ValidateBudget(child.Items, budget);
        }
    }

    private static List<Row> Shape(IReadOnlyList<object?[]> data, RenderedSqlProjection plan)
    {
        return data.Select(values =>
        {
            if (values.Length != plan.Columns.Count) throw new ArgumentException("Row values must match the projection column count.");
            var fields = plan.Fields.ToDictionary(f => f.OutputName, f => values[f.Ordinal], StringComparer.Ordinal);
            foreach (var child in plan.Collections) fields.Add(child.OutputName, new List<IReadOnlyDictionary<string, object?>>());
            return new Row(fields, new CompositeProjectionKey(plan.KeyOrdinals.Select(i => values[i])),
                plan.ParentKeyOrdinals.Count == 0 ? null : new CompositeProjectionKey(plan.ParentKeyOrdinals.Select(i => values[i])));
        }).ToList();
    }

    private async Task ChildrenAsync(List<Row> rows, RenderedSqlProjection plan, IProjectionSqlQueryExecutor io,
        SortDirective? sort, string[] path, int budget, CancellationToken cancellationToken)
    {
        var byKey = rows.GroupBy(r => r.Key, plan.KeyComparer).ToDictionary(g => g.Key, g => g.ToArray(), plan.KeyComparer);
        var keys = byKey.Keys.Where(k => k.IsComplete).ToArray();
        foreach (var child in plan.Collections)
        {
            var childPath = path.Concat(new[] { child.Name }).ToArray();
            var size = budget / child.Items.ParentKeyOrdinals.Count;
            for (var start = 0; start < keys.Length; start += size)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var predicate = _projection.RenderProjectionKeys(child.Mapping.ParentKeys, keys.Skip(start).Take(size).ToArray());
                var order = child.Mapping.DefaultOrderBy;
                var nested = sort;
                foreach (var name in childPath)
                    nested = nested?.Nested.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))?.Directive;
                if (nested is not null && nested.Items.Count > 0)
                {
                    if (child.Mapping.QueryMapping is null) throw new ArgumentException("Collection sort requires a query mapping.");
                    var rendered = _clauses.RenderOrderByClause(nested, child.Mapping.QueryMapping, "childOrder");
                    order = rendered.orderByClause;
                    foreach (var parameter in rendered.parameters) predicate.parameters.Add(parameter.Key, parameter.Value);
                }
                var sql = "SELECT " + child.Items.SelectList + " FROM " + child.Mapping.FromClause + " WHERE " + predicate.whereClause +
                    (string.IsNullOrEmpty(order) ? "" : " ORDER BY " + order);
                var children = Shape(await io.ReadAsync(sql, predicate.parameters, child.Items, cancellationToken).ConfigureAwait(false), child.Items);
                var seen = new HashSet<(CompositeProjectionKey?, CompositeProjectionKey)>(new RelationshipComparer(plan.KeyComparer, child.Items.KeyComparer));
                children = children.Where(r => !r.Key.IsComplete || seen.Add((r.Parent, r.Key))).ToList();
                await ChildrenAsync(children, child.Items, io, sort, childPath, budget, cancellationToken).ConfigureAwait(false);
                foreach (var row in children)
                    if (row.Parent is { } parent && byKey.TryGetValue(parent, out var targets))
                        foreach (var target in targets)
                            ((List<IReadOnlyDictionary<string, object?>>)target.Values[child.OutputName]!).Add(row.Values);
            }
        }
    }

    private sealed class RelationshipComparer : IEqualityComparer<(CompositeProjectionKey? Parent, CompositeProjectionKey Item)>
    {
        private readonly IEqualityComparer<CompositeProjectionKey> _parents, _items;
        internal RelationshipComparer(IEqualityComparer<CompositeProjectionKey> parents, IEqualityComparer<CompositeProjectionKey> items)
        { _parents = parents; _items = items; }
        public bool Equals((CompositeProjectionKey? Parent, CompositeProjectionKey Item) left,
            (CompositeProjectionKey? Parent, CompositeProjectionKey Item) right) =>
            _parents.Equals(left.Parent!, right.Parent!) && _items.Equals(left.Item, right.Item);
        public int GetHashCode((CompositeProjectionKey? Parent, CompositeProjectionKey Item) value) =>
            unchecked((value.Parent is null ? 0 : _parents.GetHashCode(value.Parent)) * 397 ^ _items.GetHashCode(value.Item));
    }

    private sealed class Row
    {
        internal Row(Dictionary<string, object?> values, CompositeProjectionKey key, CompositeProjectionKey? parent)
        {
            Values = values;
            Key = key;
            Parent = parent;
        }
        internal Dictionary<string, object?> Values { get; }
        internal CompositeProjectionKey Key { get; }
        internal CompositeProjectionKey? Parent { get; }
    }
}
