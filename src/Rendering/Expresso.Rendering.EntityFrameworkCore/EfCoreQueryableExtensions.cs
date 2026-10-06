using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Expresso.Core.Sorting;
using Expresso.Rendering.Linq;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore
{
    /// <summary>EF Core query helpers for Expresso sort directives.</summary>
    public static class EfCoreQueryableExtensions
    {
        private static readonly MethodInfo IncludeMethod = typeof(EntityFrameworkQueryableExtensions).GetMethods()
            .Single(m => m.Name == nameof(EntityFrameworkQueryableExtensions.Include) && m.GetGenericArguments().Length == 2);

        private static readonly MethodInfo ThenIncludeAfterCollection = typeof(EntityFrameworkQueryableExtensions).GetMethods()
            .Single(m => m.Name == nameof(EntityFrameworkQueryableExtensions.ThenInclude) && IsCollectionIncludable(m.GetParameters()[0].ParameterType));

        private static readonly MethodInfo BuildSortKeysMethod =
            typeof(IExpressionToLinqTransformer).GetMethod(nameof(IExpressionToLinqTransformer.BuildSortKeys))!;

        /// <summary>
        /// Adds a filtered <c>Include(e =&gt; e.Nav.OrderBy(...).ThenBy(...))</c>, with <c>ThenInclude</c> for deeper paths,
        /// for every nested <c>sortfor</c> directive, so loaded collections arrive in directive order. Parent keys are not
        /// applied; order the parents with <see cref="LinqQueryExtensions"/> <c>OrderBy</c>.
        /// </summary>
        /// <typeparam name="T">Root entity type.</typeparam>
        /// <param name="source">EF Core query.</param>
        /// <param name="transformer">Usually an <see cref="EfCoreExpressionToLinqTransformer"/> for the context's provider.</param>
        /// <param name="sort">Directive whose <see cref="SortDirective.Nested"/> entries are included.</param>
        /// <param name="mapping">Root mapping; every nested name must be one of its (or its items') collections.</param>
        /// <returns>The query with the sorted includes.</returns>
        /// <exception cref="ArgumentException">A nested directive names an unmapped collection.</exception>
        /// <exception cref="NotSupportedException">
        /// A collection is not mapped to a navigation property (<c>e =&gt; e.Nav</c>); order a child query with <c>OrderBy</c> instead.
        /// </exception>
        public static IQueryable<T> IncludeSorted<T>(
            this IQueryable<T> source,
            IExpressionToLinqTransformer transformer,
            SortDirective sort,
            LinqQueryMapping<T> mapping)
            where T : class
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (transformer is null)
            {
                throw new ArgumentNullException(nameof(transformer));
            }

            if (sort is null)
            {
                throw new ArgumentNullException(nameof(sort));
            }

            if (mapping is null)
            {
                throw new ArgumentNullException(nameof(mapping));
            }

            var navigationCache = new Dictionary<IncludeCacheKey, LambdaExpression>();

            foreach (var path in Paths(sort.Nested, mapping))
            {
                object query = source;
                Type? previousItem = null;
                foreach (var (collection, directive) in path)
                {
                    var include = OrderedNavigation(transformer, collection, directive, navigationCache);
                    query = previousItem is null
                        ? Invoke(IncludeMethod.MakeGenericMethod(typeof(T), include.ReturnType), query, include)
                        : Invoke(ThenIncludeAfterCollection.MakeGenericMethod(typeof(T), previousItem, include.ReturnType), query, include);
                    previousItem = collection.Items.EntityType;
                }

                source = (IQueryable<T>)query;
            }

            return source;
        }

        /// <summary>Root-to-leaf chains of nested directives (one <c>Include</c> chain each).</summary>
        private static IEnumerable<List<(LinqCollectionMapping Collection, SortDirective Directive)>> Paths(
            IReadOnlyList<CollectionSort> nested,
            LinqQueryMapping mapping)
        {
            foreach (var entry in nested)
            {
                if (!mapping.Collections.TryGetValue(entry.Name, out var collection))
                {
                    throw new ArgumentException($"No mapping for the {entry.Name} collection");
                }

                var step = (collection, entry.Directive);
                if (entry.Directive.Nested.Count == 0)
                {
                    yield return new List<(LinqCollectionMapping, SortDirective)> { step };
                    continue;
                }

                foreach (var tail in Paths(entry.Directive.Nested, collection.Items))
                {
                    tail.Insert(0, step);
                    yield return tail;
                }
            }
        }

        /// <summary><c>owner =&gt; owner.Nav.OrderBy(k1).ThenBy(k2)</c> typed as <c>Func&lt;TOwner, IEnumerable&lt;TItem&gt;&gt;</c>.</summary>
        private static LambdaExpression OrderedNavigation(
            IExpressionToLinqTransformer transformer,
            LinqCollectionMapping collection,
            SortDirective directive,
            Dictionary<IncludeCacheKey, LambdaExpression> cache)
        {
            var key = new IncludeCacheKey(collection, directive);
            if (cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var navigation = collection.Navigation;
            var body = navigation.Body is UnaryExpression { NodeType: ExpressionType.Convert } convert ? convert.Operand : navigation.Body;
            if (body is not MemberExpression { Expression: ParameterExpression })
            {
                throw new NotSupportedException(
                    $"IncludeSorted needs the '{collection.Name}' collection mapped to a navigation property (e => e.Nav). " +
                    "Order a child query with OrderBy instead.");
            }

            var itemType = collection.Items.EntityType;
            var ordered = body;
            if (directive.Items.Count > 0)
            {
                var keys = (IReadOnlyList<LinqSortKey>)Invoke(BuildSortKeysMethod.MakeGenericMethod(itemType), transformer, directive, collection.Items);
                for (var i = 0; i < keys.Count; i++)
                {
                    ordered = Expression.Call(typeof(Enumerable), MethodName(i == 0, keys[i].Direction), new[] { itemType, keys[i].Key.ReturnType }, ordered, keys[i].Key);
                }
            }

            var delegateType = typeof(Func<,>).MakeGenericType(navigation.Parameters[0].Type, typeof(IEnumerable<>).MakeGenericType(itemType));
            var lambda = Expression.Lambda(delegateType, ordered, navigation.Parameters);
            cache[key] = lambda;
            return lambda;
        }

        /// <summary>One ordered navigation per collection and directive instance.</summary>
        private readonly struct IncludeCacheKey : IEquatable<IncludeCacheKey>
        {
            public IncludeCacheKey(LinqCollectionMapping collection, SortDirective directive)
            {
                Collection = collection;
                Directive = directive;
            }

            private LinqCollectionMapping Collection { get; }

            private SortDirective Directive { get; }

            public bool Equals(IncludeCacheKey other) => ReferenceEquals(Collection, other.Collection) && ReferenceEquals(Directive, other.Directive);

            public override bool Equals(object? obj) => obj is IncludeCacheKey other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(Collection), RuntimeHelpers.GetHashCode(Directive));
        }

        private static string MethodName(bool first, SortDirection direction) =>
            (first, direction == SortDirection.Descending) switch
            {
                (true, false) => nameof(Enumerable.OrderBy),
                (true, true) => nameof(Enumerable.OrderByDescending),
                (false, false) => nameof(Enumerable.ThenBy),
                _ => nameof(Enumerable.ThenByDescending),
            };

        private static bool IsCollectionIncludable(Type sourceType)
        {
            var previous = sourceType.GetGenericArguments()[1];
            return previous.IsGenericType && previous.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }

        private static object Invoke(MethodInfo method, object target, params object[] arguments)
        {
            try
            {
                return method.IsStatic
                    ? method.Invoke(null, new[] { target }.Concat(arguments).ToArray())!
                    : method.Invoke(target, arguments)!;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }
    }
}
