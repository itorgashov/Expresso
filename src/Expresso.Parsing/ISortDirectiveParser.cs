using Expresso.Core.Sorting;

namespace Expresso.Parsing
{
    public interface ISortDirectiveParser
    {
        SortDirective Parse(string query, (string, Type)[] validFields);
    }
}
