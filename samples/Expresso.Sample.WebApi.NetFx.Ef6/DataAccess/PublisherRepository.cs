using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Sorting;
using Expresso.Rendering.Linq;
using Expresso.Sample.WebApi.NetFx.Ef6.Entities;

namespace Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

public sealed class PublisherRepository : IRepository<Publisher>
{
    private readonly SampleEf6Context _db;
    private readonly IExpressionToLinqTransformer _transformer;

    public PublisherRepository(SampleEf6Context db, IExpressionToLinqTransformer transformer)
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

        query = Ef6ListQuery.OrderAndPage(query, sortDirective, paging, _transformer, BookLinqMappings.Publishers, p => p.Id, _db);

        var list = await query.ToListAsync(cancellationToken);
        PublisherTimes.Apply(_db, list);
        return list;
    }

    public Task<long> CountAsync(FilterCriteria? filterCriteria, CancellationToken cancellationToken = default) =>
        Ef6ListQuery.CountAsync(_db.Publishers, filterCriteria, BookLinqMappings.Publishers, _transformer, cancellationToken);

    public async Task<Publisher?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var publisher = await _db.Publishers.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (publisher is not null)
        {
            PublisherTimes.Apply(_db, new[] { publisher });
        }

        return publisher;
    }
}
