using System.Linq.Expressions;
using Expresso.Core.Filtering;
using Expresso.Core.Paging;
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
        /// (NULL last ascending, ordinal strings). Use <see cref="InMemoryExpressionToLinqTransformer"/>; EF Core and EF6
        /// transformers emit SQL-only functions that cannot run after <see cref="Enumerable.OrderBy{TSource,TKey}(IEnumerable{TSource}, Func{TSource, TKey})"/> compiles the lambda.
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
        /// Orders child items in memory by the nested <c>sortfor</c> directive at <paramref name="path"/>; returns
        /// <paramref name="items"/> unchanged when there is none. Requires <see cref="InMemoryExpressionToLinqTransformer"/>.
        /// For database-backed collections use the <see cref="OrderByNested{TItem}(IQueryable{TItem}, IExpressionToLinqTransformer, SortDirective?, LinqQueryMapping{TItem}, string[])"/> overload or EF Core <c>IncludeSorted</c>.
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

        /// <summary>
        /// Applies <paramref name="paging"/> with <c>Skip</c> and <c>Take</c>. Counts are read from a captured
        /// <see cref="ParameterBox{T}"/> so EF Core and EF6 bind them as parameters. An empty directive returns <paramref name="source"/>.
        /// <c>Skip</c> is omitted when the offset is 0, so a take-only query stays legal on unsorted EF6 input.
        /// IBM EF Core 8 drops a non-zero offset; call <c>EfCorePaging.Page</c> with the provider name, which throws.
        /// SQLite EF6 rejects <c>OFFSET</c> without <c>LIMIT</c>; call <c>Ef6Paging.Page</c> with <c>Ef6Provider.Sqlite</c>.
        /// </summary>
        /// <param name="source">Query to window.</param>
        /// <param name="paging">Paging window.</param>
        /// <returns>The windowed query, or <paramref name="source"/> when <paramref name="paging"/> is empty.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="paging"/> is <see langword="null"/>.</exception>
        public static IQueryable<T> Page<T>(this IQueryable<T> source, PagingDirective paging)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (paging is null)
            {
                throw new ArgumentNullException(nameof(paging));
            }

            if (paging.IsEmpty)
            {
                return source;
            }

            var expression = source.Expression;
            if (paging.Offset > 0)
            {
                expression = Expression.Call(typeof(Queryable), nameof(Queryable.Skip), new[] { typeof(T) }, expression, Count(paging.Offset));
            }

            if (paging.Limit is int limit)
            {
                expression = Expression.Call(typeof(Queryable), nameof(Queryable.Take), new[] { typeof(T) }, expression, Count(limit));
            }

            return source.Provider.CreateQuery<T>(expression);
        }

        /// <summary>In-memory form of <see cref="Page{T}(IQueryable{T}, PagingDirective)"/>.</summary>
        /// <param name="source">Sequence to window.</param>
        /// <param name="paging">Paging window.</param>
        /// <returns>The windowed sequence, or <paramref name="source"/> when <paramref name="paging"/> is empty.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="paging"/> is <see langword="null"/>.</exception>
        public static IEnumerable<T> Page<T>(this IEnumerable<T> source, PagingDirective paging)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (paging is null)
            {
                throw new ArgumentNullException(nameof(paging));
            }

            if (paging.IsEmpty)
            {
                return source;
            }

            if (paging.Offset > 0)
            {
                source = Enumerable.Skip(source, paging.Offset);
            }

            if (paging.Limit is int limit)
            {
                source = Enumerable.Take(source, limit);
            }

            return source;
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

        private static Expression Count(int value)
        {
            var box = new ParameterBox<int>(value);
            return Expression.Field(Expression.Constant(box), nameof(ParameterBox<int>.Value));
        }
    }
}
