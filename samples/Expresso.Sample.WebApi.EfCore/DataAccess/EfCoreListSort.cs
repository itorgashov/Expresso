using System.Linq.Expressions;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Paging;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Sample.WebApi.EfCore.DataAccess;

/// <summary>Projects root pages; DB2 reads ordered keys then bounded DTO batches without CASE rank parameters.</summary>
internal static class EfCoreListSort
{
    internal static async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadAsync<T, TResult>(
        IQueryable<T> query, IQueryable<T> keySource, SortDirective? sort, LinqQueryMapping<T> mapping,
        IExpressionToLinqTransformer transformer, EfCoreProvider provider, PagingDirective paging,
        ProjectionDirective projection, LinqProjectionMapping<T, TResult> projectionMapping,
        CancellationToken cancellationToken, IProjectionQueryExecutor? executor = null, int keyBudget = 500)
        where T : class where TResult : new()
    {
        if (keyBudget <= 0) throw new ArgumentOutOfRangeException(nameof(keyBudget));
        var io = executor ?? new Executor();
        var id = IdSelector<T>();
        var effective = (sort ?? new SortDirective(Array.Empty<SortDirectiveItem>())).ThenBy(new Field("id", typeof(int)));
        var projectionTransformer = transformer as IProjectionToLinqTransformer ?? new QueryableExpressionToLinqTransformer();
        var plan = projectionTransformer.RenderProjection(projection, projectionMapping);
        var options = new ProjectionExecutionOptions { MaxKeyParameters = keyBudget };
        if (provider != EfCoreProvider.Db2)
        {
            var source = query.OrderBy(transformer, effective, mapping).Page(paging).AsNoTracking();
            return ProjectionShaper.ToSparseObjects(await plan.ExecuteAsync(source, io, transformer, sort, options,
                cancellationToken, keySource.AsNoTracking()));
        }

        var orderedKeys = await io.ToListAsync(query.OrderedKeys(transformer, effective, mapping, id), cancellationToken);
        var ids = orderedKeys.Page(paging).ToArray();
        var rows = new Dictionary<int, IReadOnlyDictionary<string, object?>>();
        for (var start = 0; start < ids.Length; start += keyBudget)
        {
            var batch = ids.Skip(start).Take(keyBudget).ToArray();
            var capture = Expression.Field(Expression.Constant(new ParameterBox<int[]>(batch)), "Value");
            var contains = Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), new[] { typeof(int) }, capture, id.Body);
            var source = keySource.Where(Expression.Lambda<Func<T, bool>>(contains, id.Parameters)).AsNoTracking();
            var result = await plan.ExecuteAsync(source, io, transformer, sort, options, cancellationToken, keySource.AsNoTracking());
            var sparse = ProjectionShaper.ToSparseObjects(result);
            for (var index = 0; index < sparse.Count; index++) rows.Add((int)result.GetKey(index)[0]!, sparse[index]);
        }
        return ids.Where(rows.ContainsKey).Select(key => rows[key]).ToArray();
    }

    private static Expression<Func<T, int>> IdSelector<T>()
    {
        var parameter = Expression.Parameter(typeof(T), "entity");
        return Expression.Lambda<Func<T, int>>(Expression.Property(parameter, "Id"), parameter);
    }

    private sealed class Executor : IProjectionQueryExecutor
    {
        public async Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) =>
            await query.ToListAsync(cancellationToken);
    }
}
