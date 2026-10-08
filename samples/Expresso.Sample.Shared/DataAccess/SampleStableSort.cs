using System.Collections.Generic;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Paging;
using Expresso.Core.Sorting;

namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Adds <c>id</c> as the last sort key so a page stays stable when the client sort is not unique.</summary>
internal static class SampleStableSort
{
    public static SortDirective? ForPaging(SortDirective? sort, PagingDirective paging)
    {
        if (paging.IsEmpty)
        {
            return sort;
        }

        return (sort ?? new SortDirective(new List<SortDirectiveItem>())).ThenBy(new Field("id", typeof(int)));
    }
}
