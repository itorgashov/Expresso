using System.Globalization;
using Expresso.Core.Paging;

namespace Expresso.Sample.WebApi.EfCore.Filtering;

/// <summary>Response headers that report how many rows matched a paged list.</summary>
public static class PagingHeaders
{
    /// <summary>Matching row count, before the page window.</summary>
    public const string TotalCount = "X-Total-Count";

    /// <summary>Page count for a page-based request. Omitted for skip/take.</summary>
    public const string TotalPages = "X-Total-Pages";

    /// <summary>Header pairs for <paramref name="paging"/>. Empty when paging itself is empty.</summary>
    public static IReadOnlyList<(string Name, string Value)> Values(PagingDirective paging, long totalCount)
    {
        if (paging.IsEmpty)
        {
            return Array.Empty<(string, string)>();
        }

        var headers = new List<(string Name, string Value)>
        {
            (TotalCount, totalCount.ToString(CultureInfo.InvariantCulture)),
        };
        if (paging.IsPageBased && paging.PageSize is int pageSize)
        {
            headers.Add((TotalPages, PagingDirective.TotalPages(totalCount, pageSize).ToString(CultureInfo.InvariantCulture)));
        }

        return headers;
    }
}
