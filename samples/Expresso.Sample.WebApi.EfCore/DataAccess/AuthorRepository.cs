using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Sorting;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;
using Expresso.Sample.WebApi.EfCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Sample.WebApi.EfCore.DataAccess;

public sealed class AuthorRepository : IRepository<Author>
{
    private readonly SampleDbContext _db;
    private readonly IExpressionToLinqTransformer _transformer;

    public AuthorRepository(SampleDbContext db, IExpressionToLinqTransformer transformer)
    {
        _db = db;
        _transformer = transformer;
    }

    public async Task<IReadOnlyList<Author>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        PagingDirective paging,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Author> query = _db.Authors;

        if (filterCriteria is not null)
        {
            query = query.Where(_transformer, filterCriteria, BookLinqMappings.Authors);
        }

        return await EfCoreListSort.ToSortedListAsync(
            query,
            sortDirective,
            BookLinqMappings.Authors,
            _transformer,
            _db,
            q => WithIncludes(q, sortDirective).AsSplitQuery(),
            paging,
            cancellationToken);
    }

    public Task<long> CountAsync(FilterCriteria? filterCriteria, CancellationToken cancellationToken = default) =>
        EfCoreCounts.LongCountAsync(_db.Authors, filterCriteria, BookLinqMappings.Authors, _transformer, cancellationToken);

    private IQueryable<Author> WithIncludes(IQueryable<Author> query, SortDirective? sortDirective)
    {
        if (sortDirective is not null && sortDirective.Nested.Count > 0)
        {
            query = query.IncludeSorted(_transformer, sortDirective, BookLinqMappings.Authors);
        }

        return query.Include(a => a.Awards);
    }

    public async Task<Author?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Authors.Include(a => a.Awards).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
}
