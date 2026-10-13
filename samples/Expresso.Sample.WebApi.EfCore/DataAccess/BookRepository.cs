using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;
using Expresso.Sample.WebApi.EfCore.Entities;

namespace Expresso.Sample.WebApi.EfCore.DataAccess;

/// <summary>Projects books using the Expresso collection executor.</summary>
public sealed class BookRepository : IRepository<Book>
{
    private readonly SampleDbContext _db;
    private readonly IExpressionToLinqTransformer _transformer;
    /// <summary>Creates the repository for the configured provider.</summary>
    public BookRepository(SampleDbContext db, IExpressionToLinqTransformer transformer)
    {
        _db = db;
        _transformer = transformer;
    }
    /// <inheritdoc />
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllAsync(FilterCriteria? filterCriteria,
        SortDirective? sortDirective, PagingDirective paging, ProjectionDirective projection, CancellationToken cancellationToken = default)
    {
        IQueryable<Book> query = _db.Books;
        if (filterCriteria is not null) query = query.Where(_transformer, filterCriteria, BookLinqMappings.Books);
        return await EfCoreListSort.ReadAsync(query, _db.Books, sortDirective, BookLinqMappings.Books, _transformer,
            EfCoreProviders.Resolve(_db.Database.ProviderName), paging, projection, SampleProjectionMappings.Books, cancellationToken);
    }
    /// <inheritdoc />
    public Task<long> CountAsync(FilterCriteria? filterCriteria, CancellationToken cancellationToken = default) =>
        EfCoreCounts.LongCountAsync(_db.Books, filterCriteria, BookLinqMappings.Books, _transformer, cancellationToken);
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, object?>?> GetByIdAsync(int id, ProjectionDirective projection, CancellationToken cancellationToken = default)
    {
        var result = await _db.Books.Where(b => b.Id == id).ToProjectedListAsync(_transformer, projection,
            SampleProjectionMappings.Books, cancellationToken: cancellationToken, keySource: _db.Books);
        return ProjectionShaper.ToSparseObjects(result).FirstOrDefault();
    }
}
