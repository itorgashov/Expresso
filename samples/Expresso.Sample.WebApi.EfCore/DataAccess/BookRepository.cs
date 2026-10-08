using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Sorting;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;
using Expresso.Sample.WebApi.EfCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Sample.WebApi.EfCore.DataAccess;

public sealed class BookRepository : IRepository<Book>
{
    private readonly SampleDbContext _db;
    private readonly IExpressionToLinqTransformer _transformer;

    public BookRepository(SampleDbContext db, IExpressionToLinqTransformer transformer)
    {
        _db = db;
        _transformer = transformer;
    }

    public async Task<IReadOnlyList<Book>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        PagingDirective paging,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Book> query = _db.Books;

        if (filterCriteria is not null)
        {
            query = query.Where(_transformer, filterCriteria, BookLinqMappings.Books);
        }

        return await EfCoreListSort.ToSortedListAsync(
            query,
            sortDirective,
            BookLinqMappings.Books,
            _transformer,
            _db,
            q => WithIncludes(q, sortDirective).AsSplitQuery(),
            paging,
            cancellationToken);
    }

    public Task<long> CountAsync(FilterCriteria? filterCriteria, CancellationToken cancellationToken = default) =>
        EfCoreCounts.LongCountAsync(_db.Books, filterCriteria, BookLinqMappings.Books, _transformer, cancellationToken);

    private IQueryable<Book> WithIncludes(IQueryable<Book> query, SortDirective? sortDirective)
    {
        if (sortDirective is not null && sortDirective.Nested.Count > 0)
        {
            query = query.IncludeSorted(_transformer, sortDirective, BookLinqMappings.Books);
        }

        return query
            .Include(b => b.Publisher)
            .Include(b => b.Authors)
            .ThenInclude(a => a.Awards);
    }

    public async Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Books
            .Include(b => b.Publisher)
            .Include(b => b.Authors)
            .ThenInclude(a => a.Awards)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
}
