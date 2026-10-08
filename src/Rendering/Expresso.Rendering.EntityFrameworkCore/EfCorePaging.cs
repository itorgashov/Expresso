using Expresso.Core.Paging;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.EntityFrameworkCore
{
    /// <summary>Provider-aware paging for EF Core. IBM EF Core 8 drops <c>OFFSET</c>.</summary>
    public static class EfCorePaging
    {
        /// <summary>
        /// Applies <paramref name="paging"/>. When <paramref name="providerName"/> is IBM EF Core and the offset is
        /// greater than zero, throws instead of returning the first page: that provider renders <c>Skip</c>/<c>Take</c>
        /// as <c>FETCH FIRST</c> and discards the offset. Materialize the ordered keys and call
        /// <see cref="LinqQueryExtensions.Page{T}(IEnumerable{T}, PagingDirective)"/>.
        /// A zero offset (first page or take-only) is <c>FETCH FIRST</c> and is translated.
        /// </summary>
        /// <param name="source">Ordered EF Core query.</param>
        /// <param name="paging">Paging window.</param>
        /// <param name="providerName"><c>Database.ProviderName</c>, such as <c>IBM.EntityFrameworkCore</c>.</param>
        /// <returns>The windowed query.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="paging"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">The provider is IBM EF Core and <see cref="PagingDirective.Offset"/> is greater than zero.</exception>
        public static IQueryable<T> Page<T>(this IQueryable<T> source, PagingDirective paging, string? providerName)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (paging is null)
            {
                throw new ArgumentNullException(nameof(paging));
            }

            if (paging.Offset > 0 && EfCoreProviders.Resolve(providerName) == EfCoreProvider.Db2)
            {
                throw new NotSupportedException(
                    "IBM EF Core 8 renders Skip and Take as FETCH FIRST and drops OFFSET. Materialize the ordered keys, then call the IEnumerable Page method.");
            }

            return source.Page(paging);
        }
    }
}
