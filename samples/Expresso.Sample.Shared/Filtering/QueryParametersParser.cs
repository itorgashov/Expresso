using Expresso.Core.Policies;
using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Sorting;
using Expresso.Parsing;

namespace Expresso.Sample.Shared.Filtering;

/// <summary>Parses sample <c>filter</c> and <c>sort</c> query strings against a field catalog.</summary>
public static class QueryParametersParser
{
    /// <summary>Outcome of parsing one request. A failed parse sets <see cref="IsBadRequest"/> and leaves the criteria unset.</summary>
    public sealed class ParseResult
    {
        /// <summary>Parsed filter, or <see langword="null"/> when the request omitted <c>filter</c>.</summary>
        public FilterCriteria? FilterCriteria { get; init; }

        /// <summary>Parsed sort with duplicate keys removed, or <see langword="null"/> when the request omitted <c>sort</c>.</summary>
        public SortDirective? SortDirective { get; init; }

        /// <summary><see langword="true"/> when the query string is invalid or a sort key was duplicated.</summary>
        public bool IsBadRequest { get; init; }

        /// <summary>The policy violation for server-side logging, when applicable.</summary>
        public QueryPolicyException? PolicyViolation { get; init; }
    }

    /// <summary>Parses <paramref name="filter"/> and <paramref name="sort"/> against the startup-compiled models.</summary>
    /// <param name="filter">Filter query string, or <see langword="null"/> when the client omitted it.</param>
    /// <param name="sort">Sort query string, or <see langword="null"/> when the client omitted it.</param>
    /// <param name="filterParser">Expresso filter parser.</param>
    /// <param name="sortDirectiveParser">Expresso sort parser.</param>
    /// <param name="filterModel">Startup filter catalog and policy.</param>
    /// <param name="sortModel">Startup sort catalog and policy.</param>
    /// <returns>The parsed criteria, or a result with <see cref="ParseResult.IsBadRequest"/> set.</returns>
    public static ParseResult Parse(
        string? filter,
        string? sort,
        IFilterParser filterParser,
        ISortDirectiveParser sortDirectiveParser,
        QueryModel filterModel,
        QueryModel sortModel)
    {
        FilterCriteria? filterCriteria = null;
        if (filter is not null)
        {
            try
            {
                filterCriteria = filterParser.Parse(filter, filterModel);
            }
            catch (QueryPolicyException ex)
            {
                return new ParseResult { IsBadRequest = true, PolicyViolation = ex };
            }
            catch
            {
                return new ParseResult { IsBadRequest = true };
            }
        }

        SortDirective? sortDirective = null;
        if (sort is not null)
        {
            try
            {
                var rawSortDirective = sortDirectiveParser.Parse(sort, sortModel);
                sortDirective = rawSortDirective.RemoveDuplicates();
                if (sortDirective.TotalSortKeyCount() < rawSortDirective.TotalSortKeyCount())
                {
                    return new ParseResult { IsBadRequest = true };
                }
            }
            catch (QueryPolicyException ex)
            {
                return new ParseResult { IsBadRequest = true, PolicyViolation = ex };
            }
            catch
            {
                return new ParseResult { IsBadRequest = true };
            }
        }

        return new ParseResult
        {
            FilterCriteria = filterCriteria,
            SortDirective = sortDirective,
        };
    }

    /// <summary>Outcome of parsing <c>page</c>, <c>pagesize</c>, <c>skip</c>, and <c>take</c>.</summary>
    public sealed class PagingParseResult
    {
        /// <summary>Parsed window. <see cref="PagingDirective.None"/> when the request is rejected or omitted paging.</summary>
        public PagingDirective Paging { get; init; } = PagingDirective.None;

        /// <summary><see langword="true"/> when a value is invalid, <c>page</c> is set without <c>pagesize</c>, or both paging styles are set.</summary>
        public bool IsBadRequest { get; init; }
    }

    /// <summary>Parses the paging query values. The library ignores a bare <c>page</c> and prefers page size over skip/take; this host rejects both of those requests.</summary>
    /// <param name="page">Raw <c>page</c> value.</param>
    /// <param name="pageSize">Raw <c>pagesize</c> value.</param>
    /// <param name="skip">Raw <c>skip</c> value.</param>
    /// <param name="take">Raw <c>take</c> value.</param>
    /// <param name="pagingParser">Expresso paging parser.</param>
    /// <returns>The window, or a result with <see cref="PagingParseResult.IsBadRequest"/> set.</returns>
    public static PagingParseResult ParsePaging(
        string? page,
        string? pageSize,
        string? skip,
        string? take,
        IPagingDirectiveParser pagingParser)
    {
        try
        {
            var paging = pagingParser.Parse(page, pageSize, skip, take);
            if ((paging.Page is not null && paging.PageSize is null) || paging.HasPageAndSkipTake)
            {
                return new PagingParseResult { IsBadRequest = true };
            }

            return new PagingParseResult { Paging = paging };
        }
        catch
        {
            return new PagingParseResult { IsBadRequest = true };
        }
    }
}
