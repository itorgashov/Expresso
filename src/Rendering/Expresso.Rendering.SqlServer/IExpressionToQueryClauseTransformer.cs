using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.SqlServer
{
    public interface IExpressionToQueryClauseTransformer
    {
        (string whereClause, Dictionary<string, object> parameters) RenderWhereClause(FilterCriteria filterCriteria, Dictionary<string, string> fieldToColumnMap, string paramNamePrefix);
        (string whereClause, Dictionary<string, object> parameters) RenderWhereClause(FilterCriteria filterCriteria, SqlQueryMapping mapping, string paramNamePrefix);
        (string orderByClause, Dictionary<string, object> parameters) RenderOrderByClause(SortDirective sortDirective, Dictionary<string, string> fieldToColumnMap, string paramNamePrefix);
        (string orderByClause, Dictionary<string, object> parameters) RenderOrderByClause(SortDirective sortDirective, SqlQueryMapping mapping, string paramNamePrefix);
    }
}
