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

public sealed class AuthorRepository : IRepository<Author>
{
    private readonly SampleEf6Context _db;
    private readonly IExpressionToLinqTransformer _transformer;

    public AuthorRepository(SampleEf6Context db, IExpressionToLinqTransformer transformer)
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

        query = Ef6ListQuery.OrderAndPage(query, sortDirective, paging, _transformer, BookLinqMappings.Authors, a => a.Id, _db);

        if (sortDirective is not null && sortDirective.Nested.Count > 0)
        {
            var authors = await query.ToListAsync(cancellationToken);
            await NestedSortLoader.AttachSortedAwardsAsync(_db, _transformer, sortDirective, authors, cancellationToken, "awards");
            return authors;
        }

        return await query.Include(a => a.Awards).ToListAsync(cancellationToken);
    }

    public Task<long> CountAsync(FilterCriteria? filterCriteria, CancellationToken cancellationToken = default) =>
        Ef6ListQuery.CountAsync(_db.Authors, filterCriteria, BookLinqMappings.Authors, _transformer, cancellationToken);

    public async Task<Author?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Authors.Include(a => a.Awards).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
}
