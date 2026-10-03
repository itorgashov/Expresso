using System.Linq.Expressions;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.Rendering.Linq
{
    /// <summary><c>Where</c> / <c>OrderBy</c> over <see cref="IQueryable{T}"/> and <see cref="IEnumerable{T}"/> from Expresso filters and sorts.</summary>
    public static class LinqQueryExtensions
    {
        /// <summary>Filters with <see cref="IExpressionToLinqTransformer.BuildPredicate{T}"/>.</summary>
        public static IQueryable<T> Where<T>(
            this IQueryable<T> source,
            IExpressionToLinqTransformer transformer,
            FilterCriteria filter,
            LinqQueryMapping<T> mapping) =>
            Queryable.Where(source, Require(transformer).BuildPredicate(filter, mapping));

        /// <summary>Filters with the compiled predicate. Use <see cref="InMemoryExpressionToLinqTransformer"/> for in-memory semantics.</summary>
        public static IEnumerable<T> Where<T>(
            this IEnumerable<T> source,
            IExpressionToLinqTransformer transformer,
            FilterCriteria filter,
            LinqQueryMapping<T> mapping) =>
            Enumerable.Where(source, Require(transformer).BuildPredicate(filter, mapping).Compile());

        /// <summary>Orders by the parent keys of <paramref name="sort"/> (<c>OrderBy</c> then <c>ThenBy</c>).</summary>
        public static IOrderedQueryable<T> OrderBy<T>(
            this IQueryable<T> source,
            IExpressionToLinqTransformer transformer,
            SortDirective sort,
            LinqQueryMapping<T> mapping)
        {
            var expression = source.Expression;
            var first = true;
            foreach (var key in Require(transformer).BuildSortKeys(sort, mapping))
            {
                var method = MethodName(first, key.Direction);
                expression = Expression.Call(typeof(Queryable), method, new[] { typeof(T), key.Key.ReturnType }, expression, Expression.Quote(key.Key));
                first = false;
            }

            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(expression);
        }

        /// <summary>
        /// Orders in memory by the parent keys of <paramref name="sort"/> with <see cref="InMemorySortComparer"/>
        /// (NULL last ascending, ordinal strings).
        /// </summary>
        public static IOrderedEnumerable<T> OrderBy<T>(
            this IEnumerable<T> source,
            IExpressionToLinqTransformer transformer,
            SortDirective sort,
            LinqQueryMapping<T> mapping)
        {
            IOrderedEnumerable<T>? ordered = null;
            foreach (var key in Require(transformer).BuildSortKeys(sort, mapping))
            {
                var boxed = Expression.Lambda<Func<T, object?>>(Expression.Convert(key.Key.Body, typeof(object)), key.Key.Parameters).Compile();
                var descending = key.Direction == SortDirection.Descending;
                ordered = ordered is null
                    ? descending
                        ? Enumerable.OrderByDescending(source, boxed, InMemorySortComparer.Instance)
                        : Enumerable.OrderBy(source, boxed, InMemorySortComparer.Instance)
                    : descending
                        ? Enumerable.ThenByDescending(ordered, boxed, InMemorySortComparer.Instance)
                        : Enumerable.ThenBy(ordered, boxed, InMemorySortComparer.Instance);
            }

            return ordered!;
        }

        /// <summary>
        /// Orders child items by the nested <c>sortfor</c> directive at <paramref name="path"/>; returns
        /// <paramref name="items"/> unchanged when there is none.
        /// </summary>
        public static IEnumerable<TItem> OrderByNested<TItem>(
            this IEnumerable<TItem> items,
            IExpressionToLinqTransformer transformer,
            SortDirective? root,
            LinqQueryMapping<TItem> itemMapping,
            params string[] path)
        {
            var nested = root.ResolveNested(path);
            return nested is null || nested.Items.Count == 0 ? items : items.OrderBy(transformer, nested, itemMapping);
        }

        /// <summary>Queryable form of <see cref="OrderByNested{TItem}(IEnumerable{TItem}, IExpressionToLinqTransformer, SortDirective?, LinqQueryMapping{TItem}, string[])"/>.</summary>
        public static IQueryable<TItem> OrderByNested<TItem>(
            this IQueryable<TItem> items,
            IExpressionToLinqTransformer transformer,
            SortDirective? root,
            LinqQueryMapping<TItem> itemMapping,
            params string[] path)
        {
            var nested = root.ResolveNested(path);
            return nested is null || nested.Items.Count == 0 ? items : items.OrderBy(transformer, nested, itemMapping);
        }

        /// <summary>Finds the nested <c>sortfor</c> directive at <paramref name="path"/> (case-insensitive), or <see langword="null"/>.</summary>
        public static SortDirective? ResolveNested(this SortDirective? root, params string[] path)
        {
            if (root is null || path is null || path.Length == 0)
            {
                return null;
            }

            var current = root;
            foreach (var segment in path)
            {
                var nested = current.Nested.FirstOrDefault(n => n.Name.Equals(segment, StringComparison.OrdinalIgnoreCase));
                if (nested is null)
                {
                    return null;
                }

                current = nested.Directive;
            }

            return current;
        }

        private static string MethodName(bool first, SortDirection direction) =>
            (first, direction == SortDirection.Descending) switch
            {
                (true, false) => nameof(Queryable.OrderBy),
                (true, true) => nameof(Queryable.OrderByDescending),
                (false, false) => nameof(Queryable.ThenBy),
                _ => nameof(Queryable.ThenByDescending),
            };

        private static IExpressionToLinqTransformer Require(IExpressionToLinqTransformer transformer) =>
            transformer ?? throw new ArgumentNullException(nameof(transformer));
    }
}
