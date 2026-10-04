using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Filtering;
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
        CancellationToken cancellationToken = default)
    {
        IQueryable<Publisher> query = _db.Publishers;

        if (filterCriteria is not null)
        {
            query = query.Where(_transformer, filterCriteria, BookLinqMappings.Publishers);
        }

        if (sortDirective is not null && sortDirective.Items.Count > 0)
        {
            query = query.OrderBy(_transformer, sortDirective, BookLinqMappings.Publishers);
        }
        else
        {
            query = query.OrderBy(p => p.Id);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Publisher?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Publishers.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
}
