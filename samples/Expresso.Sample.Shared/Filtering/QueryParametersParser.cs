using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Parsing;

namespace Expresso.Sample.Shared.Filtering;

public static class QueryParametersParser
{
    public sealed class ParseResult
    {
        public FilterCriteria? FilterCriteria { get; init; }
        public SortDirective? SortDirective { get; init; }
        public bool IsBadRequest { get; init; }
    }

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
