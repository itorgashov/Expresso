using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.Sample.WebApi.DataAccess;

public interface IRepository<T>
{
    Task<IReadOnlyList<T>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        CancellationToken cancellationToken = default);

    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
