using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;
using Expresso.Rendering.EntityFramework;
using Expresso.Rendering.Linq;
using Expresso.Sample.WebApi.NetFx.Ef6.Entities;

namespace Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

/// <summary>Projects books using the Expresso collection executor.</summary>
public sealed class BookRepository : IRepository<Book>
{
    private readonly SampleEf6Context _db;
    private readonly IExpressionToLinqTransformer _transformer;
    /// <summary>Creates the repository for the configured provider.</summary>
    public BookRepository(SampleEf6Context db, IExpressionToLinqTransformer transformer)
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
        query = Ef6ListQuery.OrderAndPage(query, sortDirective, paging, _transformer, BookLinqMappings.Books, b => b.Id, _db);
        var result = await query.ToProjectedListAsync(_transformer, projection, SampleProjectionMappings.Books, sortDirective,
            cancellationToken: cancellationToken, keySource: _db.Books);
        return ProjectionShaper.ToSparseObjects(result);
    }
    /// <inheritdoc />
    public Task<long> CountAsync(FilterCriteria? filterCriteria, CancellationToken cancellationToken = default) =>
        Ef6ListQuery.CountAsync(_db.Books, filterCriteria, BookLinqMappings.Books, _transformer, cancellationToken);
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, object?>?> GetByIdAsync(int id, ProjectionDirective projection, CancellationToken cancellationToken = default)
    {
        var result = await _db.Books.Where(b => b.Id == id).ToProjectedListAsync(_transformer, projection,
            SampleProjectionMappings.Books, cancellationToken: cancellationToken, keySource: _db.Books);
        return ProjectionShaper.ToSparseObjects(result).FirstOrDefault();
    }
}
