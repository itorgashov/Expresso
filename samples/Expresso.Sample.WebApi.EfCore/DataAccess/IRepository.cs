using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Sorting;

namespace Expresso.Sample.WebApi.EfCore.DataAccess;

public interface IRepository<T>
{
    Task<IReadOnlyList<T>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        PagingDirective paging,
        CancellationToken cancellationToken = default);

    Task<long> CountAsync(FilterCriteria? filterCriteria, CancellationToken cancellationToken = default);

    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
