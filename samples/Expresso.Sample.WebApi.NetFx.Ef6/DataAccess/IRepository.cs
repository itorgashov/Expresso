using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

public interface IRepository<T>
{
    Task<IReadOnlyList<T>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        CancellationToken cancellationToken = default);

    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
