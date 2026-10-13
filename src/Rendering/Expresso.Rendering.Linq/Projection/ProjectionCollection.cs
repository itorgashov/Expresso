using System.Linq.Expressions;
using System.Reflection;
using Expresso.Core.Filtering;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;

namespace Expresso.Rendering.Linq;

internal abstract class ProjectionCollection
{
    protected ProjectionCollection(string name, string outputName, MemberInfo target, LinqProjectionMapping items)
    {
        Name = name;
        OutputName = outputName;
        Target = target;
        Items = items;
    }
    internal string Name { get; }
    internal string OutputName { get; }
    internal MemberInfo Target { get; }
    internal LinqProjectionMapping Items { get; }
    internal abstract IEnumerable<object> Read(object parent);
    internal abstract QueryModel GetModel();
    internal abstract IQueryable ItemScope(IQueryable source);
    internal abstract Task LoadAsync(IQueryable source, LinqProjectionMapping parentMapping, List<IProjectedRow> parents,
        ProjectionDirective directive, IProjectionQueryExecutor executor, IExpressionToLinqTransformer? transformer,
        SortDirective? sort, string[] path, ProjectionExecutionOptions options, CancellationToken cancellationToken,
        IProjectionEntitySourceProvider? entitySources);
}

internal sealed class ProjectionCollection<TParent, TParentResult, TItem, TResult> : ProjectionCollection where TResult : new()
    where TParentResult : new()
{
    private readonly Expression<Func<TParent, IEnumerable<TItem>>> _navigation;
    private readonly Func<TParentResult, ICollection<TResult>> _target;
    private readonly LinqProjectionMapping<TItem, TResult> _items;
    private readonly LinqQueryMapping<TItem> _queryMapping;
    private readonly SortDirective? _defaultSort;
    internal ProjectionCollection(string name, string outputName, MemberInfo targetMember, Expression<Func<TParent, IEnumerable<TItem>>> navigation,
        Func<TParentResult, ICollection<TResult>> target, LinqProjectionMapping<TItem, TResult> items,
        LinqQueryMapping<TItem> queryMapping, SortDirective? defaultSort) : base(name, outputName, targetMember, items)
    {
        _navigation = navigation;
        _target = target;
        _items = items;
        _queryMapping = queryMapping;
        _defaultSort = defaultSort;
    }
    internal override IEnumerable<object> Read(object parent) => _target((TParentResult)parent).Cast<object>();
    internal override QueryModel GetModel() => _items.GetProjectionModel();
    internal override IQueryable ItemScope(IQueryable source) => ((IQueryable<TParent>)source).SelectMany(_navigation);

    internal override async Task LoadAsync(IQueryable source, LinqProjectionMapping parentMapping, List<IProjectedRow> parents,
        ProjectionDirective directive, IProjectionQueryExecutor executor, IExpressionToLinqTransformer? transformer,
        SortDirective? sort, string[] path, ProjectionExecutionOptions options, CancellationToken cancellationToken,
        IProjectionEntitySourceProvider? entitySources)
    {
        // DTO constructor defaults must not become rows in a requested collection, even for empty/incomplete identities.
        foreach (var parent in parents) _target((TParentResult)parent.ItemObject).Clear();
        var groups = parents.GroupBy(p => p.KeyValue).ToDictionary(g => g.Key, g => g.ToArray());
        var keys = groups.Keys.Where(k => k.IsComplete).ToArray();
        if (keys.Length == 0) return;
        var batchSize = options.MaxKeyParameters / parentMapping.Keys.Count;
        if (batchSize == 0) throw new ArgumentException("Parameter budget is smaller than one composite key.");
        // Normalize before deriving deeper sources, so shared paths cannot propagate duplicate entities.
        var parentsSource = ProjectionParentSource.Unique((IQueryable<TParent>)source, parentMapping.Keys, entitySources,
            normalizeFallback: path.Length > 1);
        // Descendants apply their own key batch. Do not retain ancestor batch parameters at every depth.
        var itemSource = Pairs(parentsSource).Select(p => p.Item);
        for (var start = 0; start < keys.Length; start += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = keys.Skip(start).Take(batchSize).ToArray();
            var filtered = parentsSource.Where(ProjectionExpressions.MatchKeys<TParent>(parentMapping.Keys, batch));
            var pairs = ApplySort(Pairs(filtered), sort, path, transformer);
            var rows = await ProjectionMaterializer.ReadAsync(pairs, RowSelector(parentMapping, directive), executor, cancellationToken).ConfigureAwait(false);
            // Shared children can occur through more than one relationship path. Attach each relationship once.
            var seen = new HashSet<(CompositeProjectionKey?, CompositeProjectionKey)>();
            rows = rows.Where(r => !r.KeyValue.IsComplete || seen.Add((r.ParentValue, r.KeyValue))).ToList();
            await _items.LoadChildrenAsync(itemSource, rows, directive, executor, transformer, sort, path, options, cancellationToken, entitySources).ConfigureAwait(false);
            foreach (var row in rows)
                if (row.ParentValue is { } parent && groups.TryGetValue(parent, out var targets))
                    foreach (var target in targets) _target((TParentResult)target.ItemObject).Add((TResult)row.ItemObject);
        }
    }

    private IQueryable<ProjectionPair<TParent, TItem>> ApplySort(IQueryable<ProjectionPair<TParent, TItem>> pairs,
        SortDirective? sort, string[] path, IExpressionToLinqTransformer? transformer)
    {
        var nestedSort = sort.ResolveNested(path);
        if (nestedSort is null || nestedSort.Items.Count == 0) nestedSort = _defaultSort;
        if (nestedSort is null || nestedSort.Items.Count == 0) return pairs;
        if (transformer is null) throw new ArgumentException("Collection sort requires a LINQ transformer.");

        // Rebind child-only sort keys to the flattened parent/item pair.
        var pair = Expression.Parameter(typeof(ProjectionPair<TParent, TItem>), "pair");
        var expressions = transformer.BuildSortKeys(nestedSort, _queryMapping);
        var ordered = pairs.Expression;
        var first = true;
        foreach (var key in expressions)
        {
            var rebound = Expression.Lambda(ProjectionExpressions.Replace(key.Key, Expression.Property(pair, "Item")), pair);
            var name = first ? (key.Direction == SortDirection.Descending ? "OrderByDescending" : "OrderBy") :
                (key.Direction == SortDirection.Descending ? "ThenByDescending" : "ThenBy");
            ordered = Expression.Call(typeof(Queryable), name, new[] { pair.Type, rebound.ReturnType }, ordered, Expression.Quote(rebound));
            first = false;
        }
        return pairs.Provider.CreateQuery<ProjectionPair<TParent, TItem>>(ordered);
    }

    private IQueryable<ProjectionPair<TParent, TItem>> Pairs(IQueryable<TParent> source)
    {
        var parent = Expression.Parameter(typeof(TParent), "parent");
        var child = Expression.Parameter(typeof(TItem), "child");
        var pairType = typeof(ProjectionPair<TParent, TItem>);
        var result = Expression.Lambda<Func<TParent, TItem, ProjectionPair<TParent, TItem>>>(
            Expression.MemberInit(Expression.New(pairType), Expression.Bind(pairType.GetProperty("Parent")!, parent),
                Expression.Bind(pairType.GetProperty("Item")!, child)), parent, child);
        return source.SelectMany(_navigation, result);
    }

    private LambdaExpression RowSelector(LinqProjectionMapping parentMapping, ProjectionDirective directive)
    {
        var pair = Expression.Parameter(typeof(ProjectionPair<TParent, TItem>), "pair");
        var item = Expression.Property(pair, "Item");
        var parent = Expression.Property(pair, "Parent");
        var key = ProjectionExpressions.Key(_items.Keys.Select(k => k.Source), item);
        var parentKey = ProjectionExpressions.Key(parentMapping.Keys.Select(k => k.Source), parent);
        var rowType = typeof(ProjectionRow<,,>).MakeGenericType(typeof(TResult), key.Type, parentKey.Type);
        var row = Expression.MemberInit(Expression.New(rowType),
            Expression.Bind(rowType.GetProperty("Item")!, ProjectionExpressions.Replace(_items.Selector(directive), item)),
            Expression.Bind(rowType.GetProperty("Key")!, key),
            Expression.Bind(rowType.GetProperty("Parent")!, parentKey));
        return Expression.Lambda(row, pair);
    }
}
