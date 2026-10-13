using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;

namespace Expresso.Rendering.Linq;

public sealed partial class LinqProjectionMapping<TEntity, TResult>
{
    internal async Task<List<IProjectedRow>> LoadRootAsync(IQueryable<TEntity> source, ProjectionDirective directive,
        IProjectionQueryExecutor executor, CancellationToken cancellationToken)
    {
        var entity = Expression.Parameter(typeof(TEntity), "entity");
        var key = ProjectionExpressions.Key(Keys.Select(k => k.Source), entity);
        var rowType = typeof(ProjectionRow<,,>).MakeGenericType(typeof(TResult), key.Type, typeof(ProjectionKeyLeaf<int>));
        var row = Expression.MemberInit(Expression.New(rowType),
            Expression.Bind(rowType.GetProperty("Item")!, ProjectionExpressions.Replace(Selector(directive), entity)),
            Expression.Bind(rowType.GetProperty("Key")!, key));
        return await ProjectionMaterializer.ReadAsync(source, Expression.Lambda(row, entity), executor, cancellationToken).ConfigureAwait(false);
    }

    internal async Task LoadChildrenAsync(IQueryable<TEntity> source, List<IProjectedRow> rows, ProjectionDirective directive,
        IProjectionQueryExecutor executor, IExpressionToLinqTransformer? transformer, SortDirective? sort, string[] path,
        ProjectionExecutionOptions options, CancellationToken cancellationToken, IProjectionEntitySourceProvider? entitySources)
    {
        if (rows.Count == 0) return;
        foreach (var collection in Collections)
        {
            var selected = directive.Find(collection.Name);
            if (!directive.IsAll && selected is null) continue;
            await collection.LoadAsync(source, this, rows, directive.IsAll ? ProjectionDirective.All : selected!.ToDirective(),
                executor, transformer, sort, path.Concat(new[] { collection.Name }).ToArray(), options, cancellationToken, entitySources).ConfigureAwait(false);
        }
    }
}

internal static class ProjectionMaterializer
{
    private static readonly MethodInfo ReadMethod = typeof(ProjectionMaterializer).GetMethod(nameof(ReadTypedAsync), BindingFlags.NonPublic | BindingFlags.Static)!;
    internal static Task<List<IProjectedRow>> ReadAsync(IQueryable source, LambdaExpression selector,
        IProjectionQueryExecutor executor, CancellationToken cancellationToken)
    {
        var call = Expression.Call(typeof(Queryable), nameof(Queryable.Select),
            new[] { source.ElementType, selector.ReturnType }, source.Expression, Expression.Quote(selector));
        var query = source.Provider.CreateQuery(call);
        try
        {
            return (Task<List<IProjectedRow>>)ReadMethod.MakeGenericMethod(selector.ReturnType)
                .Invoke(null, new object[] { query, executor, cancellationToken })!;
        }
        catch (TargetInvocationException e)
        {
            ExceptionDispatchInfo.Capture(e.InnerException!).Throw();
            throw;
        }
    }
    private static async Task<List<IProjectedRow>> ReadTypedAsync<T>(IQueryable<T> query,
        IProjectionQueryExecutor executor, CancellationToken cancellationToken) =>
        (await executor.ToListAsync(query, cancellationToken).ConfigureAwait(false)).Cast<IProjectedRow>().ToList();
}
