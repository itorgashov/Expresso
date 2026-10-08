using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Sorting;

namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Reads one sample entity, optionally filtered and sorted.</summary>
/// <typeparam name="T">Entity type loaded by the repository.</typeparam>
public interface IRepository<T>
{
    /// <summary>Returns matching rows, with related collections loaded. An empty <paramref name="paging"/> returns every match.</summary>
    /// <param name="filterCriteria">Parsed filter, or <see langword="null"/> to return all rows.</param>
    /// <param name="sortDirective">Parsed sort, or <see langword="null"/> to use the repository default order.</param>
    /// <param name="paging">Page or skip/take window. Empty means no limit.</param>
    /// <param name="cancellationToken">Token that cancels the database calls.</param>
    /// <returns>The matching entities.</returns>
    Task<IReadOnlyList<T>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        PagingDirective paging,
        CancellationToken cancellationToken = default);

    /// <summary>Counts rows that match <paramref name="filterCriteria"/>, ignoring sort and paging.</summary>
    /// <param name="filterCriteria">Parsed filter, or <see langword="null"/> to count every row.</param>
    /// <param name="cancellationToken">Token that cancels the database calls.</param>
    /// <returns>The matching row count.</returns>
    Task<long> CountAsync(FilterCriteria? filterCriteria, CancellationToken cancellationToken = default);

    /// <summary>Returns one entity by primary key.</summary>
    /// <param name="id">Primary key.</param>
    /// <param name="cancellationToken">Token that cancels the database calls.</param>
    /// <returns>The entity, or <see langword="null"/> when no row has that key.</returns>
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
