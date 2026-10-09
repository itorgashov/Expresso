using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.Parsing
{
    public interface ISortDirectiveParser
    {
        /// <summary>Parses a sort directive using the supplied fields.</summary>
        /// <remarks>This overload has no policy. Use <see cref="Parse(string, QueryModel)"/> to enforce a compiled policy.</remarks>
        SortDirective Parse(string query, (string, Type)[] validFields);
        SortDirective Parse(string query, QueryModel queryModel);
    }
}
