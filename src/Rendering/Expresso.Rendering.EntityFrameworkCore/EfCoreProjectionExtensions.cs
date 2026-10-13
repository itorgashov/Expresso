using Expresso.Core.Projection;
using Expresso.Core.Sorting;
using Expresso.Rendering.Linq;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore;

/// <summary>Executes minimal typed projections and batched collection queries with EF Core.</summary>
public static class EfCoreProjectionExtensions
{
    /// <summary>Projects a filtered/ordered/paged root query and loads only requested collection levels.</summary>
    /// <param name="source">Root query with filter, root ordering, and paging already applied.</param>
    /// <param name="transformer">Provider-specific Expresso transformer, also used for child sort keys.</param>
    /// <param name="projection">Validated public projection.</param>
    /// <param name="mapping">Typed fields, ordered key components, and collection mappings.</param>
    /// <param name="sort">Root directive whose nested sortfor scopes order child queries.</param>
    /// <param name="options">Scalar-key parameter budget; null uses the portable default.</param>
    /// <param name="cancellationToken">Cancels root and child query execution.</param>
    /// <param name="keySource">Unwindowed authorized entity source for child key lookup; defaults to source without outer ordering/paging, retaining filters.</param>
    /// <returns>Typed DTOs and sparse response-shaping metadata.</returns>
    /// <exception cref="InvalidOperationException">A requested collection's implicit source retains outer Skip/Take behind
    /// an unsupported operator. Supply an explicit authorized, unpaged <paramref name="keySource"/>.</exception>
    /// <exception cref="NotSupportedException">Root, key, or selected collection mappings use a SQL Server temporal query scope, including captured queries,
    /// or an opaque query-producing expression that cannot be safely inspected without invoking application code.
    /// Scalar-only temporal projection is supported; historical graph loading must be explicit in the host.</exception>
    public static Task<ProjectedResult<TResult>> ToProjectedListAsync<TEntity, TResult>(
        this IQueryable<TEntity> source, IExpressionToLinqTransformer transformer,
        ProjectionDirective projection, LinqProjectionMapping<TEntity, TResult> mapping,
        SortDirective? sort = null, ProjectionExecutionOptions? options = null,
        CancellationToken cancellationToken = default, IQueryable<TEntity>? keySource = null) where TEntity : class where TResult : new()
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (transformer is null) throw new ArgumentNullException(nameof(transformer));
        var projectionTransformer = transformer as IProjectionToLinqTransformer ?? new QueryableExpressionToLinqTransformer();
        var plan = projectionTransformer.RenderProjection(projection, mapping);
        return plan.ExecuteAsync(source.AsNoTracking(), new Executor(), transformer, sort, options, cancellationToken, keySource?.AsNoTracking());
    }

    private sealed class Executor : IProjectionQueryExecutor, IProjectionEntitySourceProvider, IProjectionSourceValidator
    {
        public void ValidateSource(IQueryable source) => EfCoreProjectionSourceValidator.Validate(source);
        public IQueryable? GetEntitySource(IQueryable source, Type entityType) => EfCoreProjectionEntitySource.Resolve(source, entityType);
        public async Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) =>
            await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
