using System.Collections.Generic;
using System.Globalization;
using Expresso.Core.Paging;

namespace Expresso.Sample.Shared.Filtering;

/// <summary>Response headers that report how many rows matched a paged list.</summary>
public static class PagingHeaders
{
    /// <summary>Matching row count, before the page window.</summary>
    public const string TotalCount = "X-Total-Count";

    /// <summary>Page count for a page-based request. Omitted for skip/take.</summary>
    public const string TotalPages = "X-Total-Pages";

    /// <summary>Header pairs for <paramref name="paging"/>. Empty when paging itself is empty.</summary>
    /// <param name="paging">Window that was applied.</param>
    /// <param name="totalCount">Rows matching the filter.</param>
    /// <returns><see cref="TotalCount"/>, and <see cref="TotalPages"/> when the request is page-based.</returns>
    public static IReadOnlyList<(string Name, string Value)> Values(PagingDirective paging, long totalCount)
    {
        if (paging.IsEmpty)
        {
            return new (string, string)[0];
        }

        var headers = new List<(string, string)>
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
