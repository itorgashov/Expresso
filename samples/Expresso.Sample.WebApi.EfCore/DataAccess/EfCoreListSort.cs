using System.Linq.Expressions;
using Expresso.Core.Sorting;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Sample.WebApi.EfCore.DataAccess;

/// <summary>DB2 cannot correlate sort keys in <c>ORDER BY</c>; other engines use normal <c>OrderBy</c>.</summary>
internal static class EfCoreListSort
{
    public static async Task<IReadOnlyList<T>> ToSortedListAsync<T>(
        IQueryable<T> query,
        SortDirective? sortDirective,
        LinqQueryMapping<T> mapping,
        IExpressionToLinqTransformer transformer,
        SampleDbContext db,
        Func<IQueryable<T>, IQueryable<T>> withIncludes,
        CancellationToken cancellationToken)
        where T : class
    {
        if (sortDirective is null || sortDirective.Items.Count == 0)
        {
            return await withIncludes(query.OrderBy(IdSelector<T>())).ToListAsync(cancellationToken);
        }

        if (EfCoreProviders.Resolve(db.Database.ProviderName) != EfCoreProvider.Db2)
        {
            return await withIncludes(query.OrderBy(transformer, sortDirective, mapping)).ToListAsync(cancellationToken);
        }

        var id = IdSelector<T>();
        var ids = await query.OrderedKeys(transformer, sortDirective, mapping, id).ToListAsync(cancellationToken);
        if (ids.Count == 0)
        {
            return Array.Empty<T>();
        }

        var items = await withIncludes(query.Where(MatchesIds<T>(ids))).ToListAsync(cancellationToken);
        return EfCoreLiftedSort.OrderByKeys(ids, items, id.Compile());
    }

    private static Expression<Func<T, int>> IdSelector<T>() where T : class
    {
        var parameter = Expression.Parameter(typeof(T), "e");
        var property = typeof(T).GetProperty("Id")
            ?? throw new InvalidOperationException(typeof(T).Name + " has no Id property for DB2 sort restoration.");
        return Expression.Lambda<Func<T, int>>(Expression.Property(parameter, property), parameter);
    }

    private static Expression<Func<T, bool>> MatchesIds<T>(IReadOnlyList<int> ids) where T : class
    {
        var selector = IdSelector<T>();
        Expression body = Expression.Constant(false);
        foreach (var value in ids)
        {
            body = Expression.OrElse(body, Expression.Equal(selector.Body, Expression.Constant(value)));
        }

        return Expression.Lambda<Func<T, bool>>(body, selector.Parameters);
    }
}
