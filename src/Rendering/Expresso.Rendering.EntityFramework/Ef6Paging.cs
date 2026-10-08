using System.Linq.Expressions;
using Expresso.Core.Paging;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.EntityFramework
{
    /// <summary>Provider-aware paging for EF6. SQLite EF6 rejects <c>OFFSET</c> without <c>LIMIT</c>.</summary>
    public static class Ef6Paging
    {
        /// <summary>
        /// Applies <paramref name="paging"/>. On SQLite, a skip with no take adds <c>LIMIT -1</c>, which SQLite
        /// treats as no upper bound (the same clause as the ADO renderer). Other providers keep a bare <c>OFFSET</c>.
        /// </summary>
        /// <param name="source">Ordered EF6 query. <c>Skip</c> requires an <c>OrderBy</c>.</param>
        /// <param name="paging">Paging window.</param>
        /// <param name="provider">Provider that will translate the query.</param>
        /// <returns>The windowed query.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="paging"/> is <see langword="null"/>.</exception>
        public static IQueryable<T> Page<T>(this IQueryable<T> source, PagingDirective paging, Ef6Provider provider)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (paging is null)
            {
                throw new ArgumentNullException(nameof(paging));
            }

            if (provider == Ef6Provider.Sqlite && paging.Offset > 0 && paging.Limit is null)
            {
                var skipped = source.Page(new PagingDirective(skip: paging.Offset));
                var limit = new ParameterBox<int>(-1);
                var take = Expression.Call(
                    typeof(Queryable),
                    nameof(Queryable.Take),
                    new[] { typeof(T) },
                    skipped.Expression,
                    Expression.Field(Expression.Constant(limit), nameof(ParameterBox<int>.Value)));
                return skipped.Provider.CreateQuery<T>(take);
            }

            return source.Page(paging);
        }
    }
}
