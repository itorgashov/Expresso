using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;

namespace Expresso.Sample.WebApi.EfCore.DataAccess;

public interface IRepository<T>
{
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        PagingDirective paging,
        ProjectionDirective projection,
        CancellationToken cancellationToken = default);

    Task<long> CountAsync(FilterCriteria? filterCriteria, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, object?>?> GetByIdAsync(int id, ProjectionDirective projection, CancellationToken cancellationToken = default);
}
