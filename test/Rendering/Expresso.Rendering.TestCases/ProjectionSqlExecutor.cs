using System.Linq.Expressions;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.TestCases;

/// <summary>Captures genuine provider SQL, then replaces its source with a constructed graph; no database is opened.</summary>
public sealed class ProjectionSqlExecutor : ProjectionSqlExecutor<ProjectionParent>
{
    public ProjectionSqlExecutor(Action<IQueryable> inspect) : base(ProjectionFixture.Data().AsQueryable(), inspect) { }
}

/// <summary>Translates actual provider queries, then evaluates them against constructed typed data without external I/O.</summary>
public class ProjectionSqlExecutor<TEntity> : IProjectionQueryExecutor
{
    private readonly IQueryable<TEntity> _data;
    private readonly Action<IQueryable> _inspect;
    private readonly Action? _afterRead;
    public List<int> RowCounts { get; } = new();
    public ProjectionSqlExecutor(IQueryable<TEntity> data, Action<IQueryable> inspect, Action? afterRead = null)
    {
        _data = data;
        _inspect = inspect;
        _afterRead = afterRead;
    }
    public Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _inspect(query);
        var rewritten = new SourceVisitor(_data).Visit(query.Expression)!;
        var rows = _data.Provider.CreateQuery<T>(rewritten).ToList();
        RowCounts.Add(rows.Count);
        _afterRead?.Invoke();
        return Task.FromResult<IReadOnlyList<T>>(rows);
    }

    private sealed class SourceVisitor : ExpressionVisitor
    {
        private readonly Dictionary<Type, IQueryable> _entitySources;
        private readonly HashSet<IQueryable> _queries = new();
        public SourceVisitor(IQueryable<TEntity> data)
        {
            _entitySources = EntitySources(data);
        }
        protected override Expression VisitExtension(Expression node) =>
            Source(node.Type)?.Expression ?? base.VisitExtension(node);

        private IQueryable? Source(Type queryType)
        {
            var query = queryType.GetInterfaces().Concat(new[] { queryType })
                .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IQueryable<>));
            return query is not null && _entitySources.TryGetValue(query.GetGenericArguments()[0], out var data) ? data : null;
        }
        protected override Expression VisitConstant(ConstantExpression node)
        {
            var data = Source(node.Type);
            if (data is null) return base.VisitConstant(node);
            if (node.Value is IQueryable query && query.Expression is not ConstantExpression && _queries.Add(query))
            {
                try { return Visit(query.Expression); }
                finally { _queries.Remove(query); }
            }
            return data.Expression;
        }
        protected override Expression VisitUnary(UnaryExpression node) =>
            node.NodeType == ExpressionType.Convert && Source(node.Type) is not null
                ? Visit(node.Operand) : base.VisitUnary(node);
        protected override Expression VisitMethodCall(MethodCallExpression node) =>
            node.Method.Name is "AsNoTracking" or "AsTracking" or "TagWith" or "Include" or "ThenInclude" or
                "IgnoreQueryFilters" or "AsSplitQuery" or "AsSingleQuery" ? Visit(node.Object ?? node.Arguments[0]) :
            node.Method.Name == "MergeAs" ? Visit(node.Object!) : base.VisitMethodCall(node);

        private static Dictionary<Type, IQueryable> EntitySources(IEnumerable<TEntity> roots)
        {
            // Model actual entity sets from the constructed graph, not relationship paths with repeated shared entities.
            var entities = new Dictionary<Type, List<object>>();
            var seen = new HashSet<object>(ReferenceComparer.Instance);
            void AddType(Type type)
            {
                if (entities.ContainsKey(type)) return;
                entities.Add(type, new List<object>());
                foreach (var property in type.GetProperties())
                {
                    if (property.PropertyType == typeof(string) || property.PropertyType == typeof(byte[])) continue;
                    var collection = property.PropertyType.GetInterfaces().Concat(new[] { property.PropertyType })
                        .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                    var itemType = collection?.GetGenericArguments()[0];
                    if (itemType is not null && !itemType.IsValueType && itemType != typeof(string)) AddType(itemType);
                }
            }
            AddType(typeof(TEntity));
            void Add(object entity)
            {
                if (!seen.Add(entity)) return;
                var type = entity.GetType();
                for (var entityType = type; entityType is not null && entityType != typeof(object); entityType = entityType.BaseType)
                {
                    AddType(entityType);
                    entities[entityType].Add(entity);
                }
                foreach (var property in type.GetProperties())
                    if (property.PropertyType != typeof(string) && property.PropertyType != typeof(byte[]) &&
                        property.GetValue(entity) is System.Collections.IEnumerable navigation)
                        foreach (var child in navigation)
                            if (child is not null && !child.GetType().IsValueType && child is not string) Add(child);
            }
            foreach (var root in roots) Add(root!);
            return entities.ToDictionary(pair => pair.Key, pair => (IQueryable)typeof(Queryable).GetMethods()
                .Single(m => m.Name == nameof(Queryable.AsQueryable) && m.IsGenericMethod)
                .MakeGenericMethod(pair.Key).Invoke(null, new[] { typeof(Enumerable).GetMethod(nameof(Enumerable.Cast))!
                    .MakeGenericMethod(pair.Key).Invoke(null, new object[] { pair.Value }) })!);
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            internal static readonly ReferenceComparer Instance = new();
            public new bool Equals(object? left, object? right) => ReferenceEquals(left, right);
            public int GetHashCode(object value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
        }
    }
}
