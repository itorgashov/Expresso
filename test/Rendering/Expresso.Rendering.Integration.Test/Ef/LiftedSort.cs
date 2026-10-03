#if NET8_0_OR_GREATER
using System.Linq.Expressions;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.Integration.Test.Ef
{
    /// <summary>Projected row: the id plus up to four sort keys.</summary>
    public sealed class LiftedRow<T0, T1, T2, T3>
    {
        public int Id { get; set; }

        public T0 K0 { get; set; } = default!;

        public T1 K1 { get; set; } = default!;

        public T2 K2 { get; set; } = default!;

        public T3 K3 { get; set; } = default!;
    }

    /// <summary>
    /// DB2 cannot correlate subqueries in <c>ORDER BY</c> (<c>SQL0206N</c>). Like the ADO session's <c>orderByInSelectList</c>,
    /// the keys are projected into a derived table and the outer query orders by them. <c>Distinct</c> forces the pushdown
    /// with the keys computed inside (rows are unique by id, so it removes nothing).
    /// </summary>
    public static class LiftedSort
    {
        private const int MaxKeys = 4;

        public static IQueryable<int> OrderedIds(IQueryable<Widget> source, IReadOnlyList<LinqSortKey> keys)
        {
            if (keys.Count > MaxKeys)
            {
                throw new NotSupportedException($"At most {MaxKeys} sort keys can be lifted.");
            }

            var widget = Expression.Parameter(typeof(Widget), "w");
            var bodies = keys.Select(k => LinqEx.Inline(k.Key, widget)).ToList();
            var rowType = typeof(LiftedRow<,,,>).MakeGenericType(
                Enumerable.Range(0, MaxKeys).Select(i => i < bodies.Count ? bodies[i].Type : typeof(int)).ToArray());

            var bindings = new List<MemberBinding> { Expression.Bind(rowType.GetProperty(nameof(Widget.Id))!, Expression.Property(widget, nameof(Widget.Id))) };
            bindings.AddRange(bodies.Select((body, i) => (MemberBinding)Expression.Bind(rowType.GetProperty("K" + i)!, body)));
            var projection = Expression.Lambda(Expression.MemberInit(Expression.New(rowType), bindings), widget);

            var expression = Expression.Call(typeof(Queryable), nameof(Queryable.Select), new[] { typeof(Widget), rowType }, source.Expression, Expression.Quote(projection));
            expression = Expression.Call(typeof(Queryable), nameof(Queryable.Distinct), new[] { rowType }, expression);

            var row = Expression.Parameter(rowType, "r");
            for (var i = 0; i < keys.Count; i++)
            {
                var key = Expression.Lambda(Expression.Property(row, "K" + i), row);
                var method = (i == 0, keys[i].Direction == Core.Sorting.SortDirection.Descending) switch
                {
                    (true, false) => nameof(Queryable.OrderBy),
                    (true, true) => nameof(Queryable.OrderByDescending),
                    (false, false) => nameof(Queryable.ThenBy),
                    _ => nameof(Queryable.ThenByDescending),
                };
                expression = Expression.Call(typeof(Queryable), method, new[] { rowType, key.ReturnType }, expression, Expression.Quote(key));
            }

            var id = Expression.Lambda(Expression.Property(row, nameof(Widget.Id)), row);
            expression = Expression.Call(typeof(Queryable), nameof(Queryable.Select), new[] { rowType, typeof(int) }, expression, Expression.Quote(id));
            return source.Provider.CreateQuery<int>(expression);
        }
    }
}
#endif
