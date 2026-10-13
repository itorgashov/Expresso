using System.Linq.Expressions;
using Expresso.Core.Projection;

namespace Expresso.Rendering.Linq;

internal static class ProjectionParentSource
{
    internal static IQueryable<T> Unique<T>(IQueryable<T> source,
        IReadOnlyList<(string Name, LambdaExpression Source)> keys, IProjectionEntitySourceProvider? provider,
        bool normalizeFallback = true)
    {
        var entitySource = provider?.GetEntitySource(source, typeof(T));
        if (ReferenceEquals(entitySource, source)) return source;
        if (entitySource is not null)
        {
            if (entitySource is not IQueryable<T> entities || entitySource.ElementType != typeof(T))
                throw new ArgumentException("Projection entity sources must match the requested entity type.");
            var entity = Expression.Parameter(typeof(T), "entity");
            var original = Expression.Parameter(typeof(T), "original");
            var keyShape = ProjectionExpressions.Key(keys.Select(k => k.Source), original);
            var keyQuery = Expression.Call(typeof(Queryable), nameof(Queryable.Select), new[] { typeof(T), keyShape.Type },
                source.Expression, Expression.Quote(Expression.Lambda(keyShape, original)));
            var candidate = Expression.Parameter(keyShape.Type, "authorizedKey");
            Expression component = candidate;
            Expression? match = null;
            for (var index = 0; index < keys.Count; index++)
            {
                var left = ProjectionExpressions.Replace(keys[index].Source, entity);
                var right = Expression.Property(component, "Value");
                var equal = ProjectionExpressions.EqualKey(left, right);
                match = match is null ? equal : Expression.AndAlso(match, equal);
                if (index < keys.Count - 1) component = Expression.Property(component, "Rest");
            }
            // EXISTS preserves the complete authorized navigation scope without comparing any non-key columns.
            var membership = Expression.Call(typeof(Queryable), nameof(Queryable.Any), new[] { keyShape.Type },
                keyQuery, Expression.Quote(Expression.Lambda(match!, candidate)));
            return entities.Where(Expression.Lambda<Func<T, bool>>(membership, entity));
        }

        // The first-level key source is caller-owned; do not impose grouping translation without a resolver.
        if (!normalizeFallback) return source;
        // LINQ-to-objects/custom providers: group structurally by declared keys, not entity equality.
        // Keep this in the provider expression; never force client-side entity materialization.
        var parent = Expression.Parameter(typeof(T), "parent");
        var values = Expression.NewArrayInit(typeof(object), keys.Select(k =>
            Expression.Convert(ProjectionExpressions.Replace(k.Source, parent), typeof(object))));
        var identity = Expression.New(typeof(CompositeProjectionKey).GetConstructor(new[] { typeof(IEnumerable<object>) })!, values);
        var selector = Expression.Lambda<Func<T, CompositeProjectionKey>>(identity, parent);
        return source.GroupBy(selector).Select(group => group.First());
    }
}
