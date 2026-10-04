using Expresso.Core.Filtering;
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
        CancellationToken cancellationToken = default)
    {
        IQueryable<Book> query = _db.Books;

        if (filterCriteria is not null)
        {
            query = query.Where(_transformer, filterCriteria, BookLinqMappings.Books);
        }

        if (sortDirective is not null && sortDirective.Items.Count > 0)
        {
            query = query.OrderBy(_transformer, sortDirective, BookLinqMappings.Books);
        }
        else
        {
            query = query.OrderBy(b => b.Id);
        }

        if (sortDirective is not null && sortDirective.Nested.Count > 0)
        {
            query = query.IncludeSorted(_transformer, sortDirective, BookLinqMappings.Books);
        }

        query = query
            .Include(b => b.Publisher)
            .Include(b => b.Authors)
            .ThenInclude(a => a.Awards);

        return await query.AsSplitQuery().ToListAsync(cancellationToken);
    }

    public async Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Books
            .Include(b => b.Publisher)
            .Include(b => b.Authors)
            .ThenInclude(a => a.Awards)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
}
