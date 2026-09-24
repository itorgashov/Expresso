using Expresso.Core.Filtering;
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
    }

    /// <summary>Parses <paramref name="filter"/> and <paramref name="sort"/> for <paramref name="context"/>.</summary>
    /// <param name="filter">Filter query string, or <see langword="null"/> when the client omitted it.</param>
    /// <param name="sort">Sort query string, or <see langword="null"/> when the client omitted it.</param>
    /// <param name="context">Field-catalog name, such as <c>book</c>, <c>author</c>, or <c>publisher</c>.</param>
    /// <param name="filterParser">Expresso filter parser.</param>
    /// <param name="sortDirectiveParser">Expresso sort parser.</param>
    /// <param name="fieldsProvider">Catalog for <paramref name="context"/>. A query-model provider is used when the host implements one.</param>
    /// <returns>The parsed criteria, or a result with <see cref="ParseResult.IsBadRequest"/> set.</returns>
    public static ParseResult Parse(
        string? filter,
        string? sort,
        string context,
        IFilterParser filterParser,
        ISortDirectiveParser sortDirectiveParser,
        IRequestFieldsInfoProvider fieldsProvider)
    {
        FilterCriteria? filterCriteria = null;
        if (filter is not null)
        {
            try
            {
                filterCriteria = fieldsProvider is IRequestQueryModelProvider modelProvider
                    ? filterParser.Parse(filter, modelProvider.GetFilterModel(context))
                    : filterParser.Parse(filter, fieldsProvider.GetValidFilterFields(context));
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
                var rawSortDirective = fieldsProvider is IRequestQueryModelProvider modelProvider
                    ? sortDirectiveParser.Parse(sort, modelProvider.GetSortModel(context))
                    : sortDirectiveParser.Parse(sort, fieldsProvider.GetValidSortFields(context));
                sortDirective = rawSortDirective.RemoveDuplicates();
                if (sortDirective.TotalSortKeyCount() < rawSortDirective.TotalSortKeyCount())
                {
                    return new ParseResult { IsBadRequest = true };
                }
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
}
