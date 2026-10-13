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

/// <summary>Projects authors using the Expresso collection executor.</summary>
public sealed class AuthorRepository : IRepository<Author>
{
    private readonly SampleEf6Context _db;
    private readonly IExpressionToLinqTransformer _transformer;
    /// <summary>Creates the repository for the configured provider.</summary>
    public AuthorRepository(SampleEf6Context db, IExpressionToLinqTransformer transformer)
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
        query = Ef6ListQuery.OrderAndPage(query, sortDirective, paging, _transformer, BookLinqMappings.Authors, a => a.Id, _db);
        var result = await query.ToProjectedListAsync(_transformer, projection, SampleProjectionMappings.Authors, sortDirective,
            cancellationToken: cancellationToken, keySource: _db.Authors);
        return ProjectionShaper.ToSparseObjects(result);
    }
    /// <inheritdoc />
    public Task<long> CountAsync(FilterCriteria? filterCriteria, CancellationToken cancellationToken = default) =>
        Ef6ListQuery.CountAsync(_db.Authors, filterCriteria, BookLinqMappings.Authors, _transformer, cancellationToken);
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, object?>?> GetByIdAsync(int id, ProjectionDirective projection, CancellationToken cancellationToken = default)
    {
        var result = await _db.Authors.Where(a => a.Id == id).ToProjectedListAsync(_transformer, projection,
            SampleProjectionMappings.Authors, cancellationToken: cancellationToken, keySource: _db.Authors);
        return ProjectionShaper.ToSparseObjects(result).FirstOrDefault();
    }
}
