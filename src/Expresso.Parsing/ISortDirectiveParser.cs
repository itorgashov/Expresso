using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.Parsing
{
    public interface ISortDirectiveParser
    {
        SortDirective Parse(string query, (string, Type)[] validFields);
        SortDirective Parse(string query, QueryModel queryModel);
    }
}
