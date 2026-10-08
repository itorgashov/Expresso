using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Sorting;
using Expresso.Rendering.Linq;
using Expresso.Sample.WebApi.EfCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Sample.WebApi.EfCore.DataAccess;

public sealed class PublisherRepository : IRepository<Publisher>
{
    private readonly SampleDbContext _db;
    private readonly IExpressionToLinqTransformer _transformer;

    public PublisherRepository(SampleDbContext db, IExpressionToLinqTransformer transformer)
    {
        _db = db;
        _transformer = transformer;
    }

    public async Task<IReadOnlyList<Publisher>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        PagingDirective paging,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Publisher> query = _db.Publishers;

        if (filterCriteria is not null)
        {
            query = query.Where(_transformer, filterCriteria, BookLinqMappings.Publishers);
        }

        return await EfCoreListSort.ToSortedListAsync(
            query,
            sortDirective,
            BookLinqMappings.Publishers,
            _transformer,
            _db,
            q => q,
            paging,
            cancellationToken);
    }

    public Task<long> CountAsync(FilterCriteria? filterCriteria, CancellationToken cancellationToken = default) =>
        EfCoreCounts.LongCountAsync(_db.Publishers, filterCriteria, BookLinqMappings.Publishers, _transformer, cancellationToken);

    public async Task<Publisher?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Publishers.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
}
