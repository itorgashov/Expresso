using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Sorting;
using Expresso.Rendering.EntityFramework;
using Expresso.Rendering.Linq;

namespace Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

internal static class Ef6ListQuery
{
    public static IQueryable<T> OrderAndPage<T>(
        IQueryable<T> query,
        SortDirective? sort,
        PagingDirective paging,
        IExpressionToLinqTransformer transformer,
        LinqQueryMapping<T> mapping,
        Expression<Func<T, int>> id,
        SampleEf6Context db)
    {
        var effective = paging.IsEmpty
            ? sort
            : (sort ?? new SortDirective(new List<SortDirectiveItem>())).ThenBy(new Field("id", typeof(int)));
        if (effective is not null && effective.Items.Count > 0)
        {
            query = query.OrderBy(transformer, effective, mapping);
        }
        else
        {
            query = query.OrderBy(id);
        }

        var provider = IsSqlite(db) ? Ef6Provider.Sqlite : Ef6Provider.Other;
        return query.Page(paging, provider);
    }

    private static bool IsSqlite(SampleEf6Context db) =>
        db.Database.Connection.GetType().FullName?.IndexOf("SQLite", StringComparison.OrdinalIgnoreCase) >= 0;

    public static async Task<long> CountAsync<T>(
        IQueryable<T> query,
        FilterCriteria? filter,
        LinqQueryMapping<T> mapping,
        IExpressionToLinqTransformer transformer,
        CancellationToken cancellationToken)
    {
        if (filter is not null)
        {
            query = query.Where(transformer, filter, mapping);
        }

        // SQLite EF6 translates LongCount to BigCount, which SQLite does not have.
        return await query.CountAsync(cancellationToken);
    }
}
