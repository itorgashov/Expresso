using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;
using Expresso.Sample.WebApi.EfCore.Entities;

namespace Expresso.Sample.WebApi.EfCore.DataAccess;

/// <summary>Projects publishers using the Expresso collection executor.</summary>
public sealed class PublisherRepository : IRepository<Publisher>
{
    private readonly SampleDbContext _db;
    private readonly IExpressionToLinqTransformer _transformer;
    /// <summary>Creates the repository for the configured provider.</summary>
    public PublisherRepository(SampleDbContext db, IExpressionToLinqTransformer transformer)
    {
        _db = db;
        _transformer = transformer;
    }
    /// <inheritdoc />
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllAsync(FilterCriteria? filterCriteria,
        SortDirective? sortDirective, PagingDirective paging, ProjectionDirective projection, CancellationToken cancellationToken = default)
    {
        IQueryable<Publisher> query = _db.Publishers;
        if (filterCriteria is not null) query = query.Where(_transformer, filterCriteria, BookLinqMappings.Publishers);
        return await EfCoreListSort.ReadAsync(query, _db.Publishers, sortDirective, BookLinqMappings.Publishers, _transformer,
            EfCoreProviders.Resolve(_db.Database.ProviderName), paging, projection, SampleProjectionMappings.Publishers, cancellationToken);
    }
    /// <inheritdoc />
    public Task<long> CountAsync(FilterCriteria? filterCriteria, CancellationToken cancellationToken = default) =>
        EfCoreCounts.LongCountAsync(_db.Publishers, filterCriteria, BookLinqMappings.Publishers, _transformer, cancellationToken);
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, object?>?> GetByIdAsync(int id, ProjectionDirective projection, CancellationToken cancellationToken = default)
    {
        var result = await _db.Publishers.Where(p => p.Id == id).ToProjectedListAsync(_transformer, projection,
            SampleProjectionMappings.Publishers, cancellationToken: cancellationToken, keySource: _db.Publishers);
        return ProjectionShaper.ToSparseObjects(result).FirstOrDefault();
    }
}
