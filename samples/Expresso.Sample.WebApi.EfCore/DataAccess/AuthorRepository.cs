using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;
using Expresso.Sample.WebApi.EfCore.Entities;

namespace Expresso.Sample.WebApi.EfCore.DataAccess;

/// <summary>Projects authors using the Expresso collection executor.</summary>
public sealed class AuthorRepository : IRepository<Author>
{
    private readonly SampleDbContext _db;
    private readonly IExpressionToLinqTransformer _transformer;
    /// <summary>Creates the repository for the configured provider.</summary>
    public AuthorRepository(SampleDbContext db, IExpressionToLinqTransformer transformer)
    {
        _db = db;
        _transformer = transformer;
    }
    /// <inheritdoc />
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllAsync(FilterCriteria? filterCriteria,
        SortDirective? sortDirective, PagingDirective paging, ProjectionDirective projection, CancellationToken cancellationToken = default)
    {
        IQueryable<Author> query = _db.Authors;
        if (filterCriteria is not null) query = query.Where(_transformer, filterCriteria, BookLinqMappings.Authors);
        return await EfCoreListSort.ReadAsync(query, _db.Authors, sortDirective, BookLinqMappings.Authors, _transformer,
            EfCoreProviders.Resolve(_db.Database.ProviderName), paging, projection, SampleProjectionMappings.Authors, cancellationToken);
    }
    /// <inheritdoc />
    public Task<long> CountAsync(FilterCriteria? filterCriteria, CancellationToken cancellationToken = default) =>
        EfCoreCounts.LongCountAsync(_db.Authors, filterCriteria, BookLinqMappings.Authors, _transformer, cancellationToken);
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, object?>?> GetByIdAsync(int id, ProjectionDirective projection, CancellationToken cancellationToken = default)
    {
        var result = await _db.Authors.Where(a => a.Id == id).ToProjectedListAsync(_transformer, projection,
            SampleProjectionMappings.Authors, cancellationToken: cancellationToken, keySource: _db.Authors);
        return ProjectionShaper.ToSparseObjects(result).FirstOrDefault();
    }
}
