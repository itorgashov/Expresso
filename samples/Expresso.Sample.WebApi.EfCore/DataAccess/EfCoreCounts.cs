using Expresso.Core.Filtering;
using Expresso.Rendering.Linq;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Sample.WebApi.EfCore.DataAccess;

internal static class EfCoreCounts
{
    public static Task<long> LongCountAsync<T>(
        IQueryable<T> source,
        FilterCriteria? filter,
        LinqQueryMapping<T> mapping,
        IExpressionToLinqTransformer transformer,
        CancellationToken cancellationToken)
    {
        if (filter is not null)
        {
            source = source.Where(transformer, filter, mapping);
        }

        return source.LongCountAsync(cancellationToken);
    }
}
