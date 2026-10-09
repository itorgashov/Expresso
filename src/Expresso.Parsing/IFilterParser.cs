using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;

namespace Expresso.Parsing
{
    public interface IFilterParser
    {
        /// <summary>Parses a filter using the supplied fields.</summary>
        /// <remarks>This overload has no policy. Use <see cref="Parse(string, QueryModel)"/> to enforce a compiled policy.</remarks>
        FilterCriteria Parse(string query, (string, Type)[] validFields);
        FilterCriteria Parse(string query, QueryModel queryModel);
    }
}
