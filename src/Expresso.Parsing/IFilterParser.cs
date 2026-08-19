using Expresso.Core.Filtering;

namespace Expresso.Parsing
{
    public interface IFilterParser
    {
        FilterCriteria Parse(string query, (string, Type)[] validFields);
    }
}
