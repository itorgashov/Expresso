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
                filterCriteria = filterParser.Parse(filter, fieldsProvider.GetValidFilterFields(context));
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
                var validFields = fieldsProvider.GetValidSortFields(context);
                var rawSortDirective = sortDirectiveParser.Parse(sort, validFields);
                sortDirective = rawSortDirective.RemoveDuplicates();
                if (sortDirective.Items.Count < rawSortDirective.Items.Count)
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
