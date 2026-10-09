using Expresso.Core.Policies;
using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Sorting;
using Expresso.Parsing;

namespace Expresso.Sample.WebApi.EfCore.Filtering;

public static class QueryParametersParser
{
    public sealed class ParseResult
    {
        public FilterCriteria? FilterCriteria { get; init; }

        public SortDirective? SortDirective { get; init; }

        public bool IsBadRequest { get; init; }

        /// <summary>The policy violation for server-side logging, when applicable.</summary>
        public QueryPolicyException? PolicyViolation { get; init; }
    }

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

    public sealed class PagingParseResult
    {
        public PagingDirective Paging { get; init; } = PagingDirective.None;

        public bool IsBadRequest { get; init; }
    }

    /// <summary>Rejects a bare <c>page</c> and a request that sets both paging styles. The library would otherwise ignore the bare page and prefer page size.</summary>
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
