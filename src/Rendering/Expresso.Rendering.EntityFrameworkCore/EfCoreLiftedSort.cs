using System.Linq.Expressions;
using Expresso.Core.Sorting;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.EntityFrameworkCore
{
    /// <summary>Projected row: the key plus up to eight sort keys.</summary>
    internal sealed class LiftedRow<TKey, T0, T1, T2, T3, T4, T5, T6, T7>
    {
        public TKey Key { get; set; } = default!;

        public T0 K0 { get; set; } = default!;

        public T1 K1 { get; set; } = default!;

        public T2 K2 { get; set; } = default!;

        public T3 K3 { get; set; } = default!;

        public T4 K4 { get; set; } = default!;

        public T5 K5 { get; set; } = default!;

        public T6 K6 { get; set; } = default!;

        public T7 K7 { get; set; } = default!;
    }

    /// <summary>
    /// DB2 cannot correlate subqueries in <c>ORDER BY</c> (<c>SQL0206N</c>). Keys are projected into a derived table,
    /// the outer query orders by them, and the key values are returned in that order.
    /// </summary>
    public static class EfCoreLiftedSort
    {
        private const int MaxKeys = 8;

        /// <summary>
        /// Returns <paramref name="key"/> values in <paramref name="sort"/> order without correlating sort expressions in
        /// <c>ORDER BY</c>.
        /// </summary>
        public static IQueryable<TKey> OrderedKeys<T, TKey>(
            this IQueryable<T> source,
            IExpressionToLinqTransformer transformer,
            SortDirective sort,
            LinqQueryMapping<T> mapping,
            Expression<Func<T, TKey>> key)
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

            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var keys = transformer.BuildSortKeys(sort, mapping);
            if (keys.Count > MaxKeys)
            {
                throw new NotSupportedException($"At most {MaxKeys} sort keys can be lifted.");
            }

            var item = Expression.Parameter(typeof(T), "e");
            var keyBody = LinqEx.Inline(key, item);
            var sortBodies = keys.Select(k => LinqEx.Inline(k.Key, item)).ToList();
            var rowType = typeof(LiftedRow<,,,,,,,,>).MakeGenericType(
                new[] { typeof(TKey) }.Concat(Enumerable.Range(0, MaxKeys).Select(i => i < sortBodies.Count ? sortBodies[i].Type : typeof(int))).ToArray());

            var bindings = new List<MemberBinding> { Expression.Bind(rowType.GetProperty("Key")!, keyBody) };
            bindings.AddRange(sortBodies.Select((body, i) => (MemberBinding)Expression.Bind(rowType.GetProperty("K" + i)!, body)));
            var projection = Expression.Lambda(Expression.MemberInit(Expression.New(rowType), bindings), item);

            var expression = Expression.Call(typeof(Queryable), nameof(Queryable.Select), new[] { typeof(T), rowType }, source.Expression, Expression.Quote(projection));
            expression = Expression.Call(typeof(Queryable), nameof(Queryable.Distinct), new[] { rowType }, expression);

            var row = Expression.Parameter(rowType, "r");
            for (var i = 0; i < keys.Count; i++)
            {
                var sortKey = Expression.Lambda(Expression.Property(row, "K" + i), row);
                var method = (i == 0, keys[i].Direction == SortDirection.Descending) switch
                {
                    (true, false) => nameof(Queryable.OrderBy),
                    (true, true) => nameof(Queryable.OrderByDescending),
                    (false, false) => nameof(Queryable.ThenBy),
                    _ => nameof(Queryable.ThenByDescending),
                };
                expression = Expression.Call(typeof(Queryable), method, new[] { rowType, sortKey.ReturnType }, expression, Expression.Quote(sortKey));
            }

            var keySelector = Expression.Lambda(Expression.Property(row, "Key"), row);
            expression = Expression.Call(typeof(Queryable), nameof(Queryable.Select), new[] { rowType, typeof(TKey) }, expression, Expression.Quote(keySelector));
            return source.Provider.CreateQuery<TKey>(expression);
        }

        /// <summary>Restores <paramref name="keys"/> order after a second query loaded <paramref name="items"/>.</summary>
        public static IReadOnlyList<T> OrderByKeys<T, TKey>(IReadOnlyList<TKey> keys, IEnumerable<T> items, Func<T, TKey> key)
            where TKey : notnull
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var byKey = new Dictionary<TKey, T>();
            foreach (var item in items)
            {
                byKey[key(item)] = item;
            }

            var ordered = new List<T>(keys.Count);
            foreach (var id in keys)
            {
                ordered.Add(byKey[id]);
            }

            return ordered;
        }
    }
}
