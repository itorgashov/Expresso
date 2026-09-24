using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.Rendering
{
    /// <summary>
    /// Renders a filter and sort IR to parameterized SQL WHERE and ORDER BY fragments.
    /// </summary>
    public interface IExpressionToQueryClauseTransformer
    {
        /// <summary>Renders <paramref name="filterCriteria"/> as a parameterized <c>WHERE</c> fragment.</summary>
        /// <param name="filterCriteria">Parsed filter. Its expression must be set.</param>
        /// <param name="fieldToColumnMap">Scalar field name to column expression. Lookup is case-insensitive.</param>
        /// <param name="paramNamePrefix">Bind-name prefix. Must start with a letter and contain only letters, digits, and underscores.</param>
        /// <returns>The <c>WHERE</c> text without the <c>WHERE</c> keyword, and the bound parameter values keyed by bind name.</returns>
        (string whereClause, Dictionary<string, object> parameters) RenderWhereClause(FilterCriteria filterCriteria, Dictionary<string, string> fieldToColumnMap, string paramNamePrefix);

        /// <summary>Renders <paramref name="filterCriteria"/> as a parameterized <c>WHERE</c> fragment, including collection subqueries.</summary>
        /// <param name="filterCriteria">Parsed filter. Its expression must be set.</param>
        /// <param name="mapping">Scalar columns and nested collection SQL.</param>
        /// <param name="paramNamePrefix">Bind-name prefix. Must start with a letter and contain only letters, digits, and underscores.</param>
        /// <returns>The <c>WHERE</c> text without the <c>WHERE</c> keyword, and the bound parameter values keyed by bind name.</returns>
        (string whereClause, Dictionary<string, object> parameters) RenderWhereClause(FilterCriteria filterCriteria, SqlQueryMapping mapping, string paramNamePrefix);

        /// <summary>Renders <paramref name="sortDirective"/> as a parameterized <c>ORDER BY</c> fragment.</summary>
        /// <param name="sortDirective">Parsed sort. <see cref="SortDirective.Items"/> must contain at least one key. Nested <c>sortfor</c> keys are ignored.</param>
        /// <param name="fieldToColumnMap">Scalar field name to column expression. Lookup is case-insensitive.</param>
        /// <param name="paramNamePrefix">Bind-name prefix. Must start with a letter and contain only letters, digits, and underscores.</param>
        /// <returns>The <c>ORDER BY</c> text without the <c>ORDER BY</c> keyword, and the bound parameter values keyed by bind name.</returns>
        (string orderByClause, Dictionary<string, object> parameters) RenderOrderByClause(SortDirective sortDirective, Dictionary<string, string> fieldToColumnMap, string paramNamePrefix);

        /// <summary>Renders <paramref name="sortDirective"/> as a parameterized <c>ORDER BY</c> fragment. Collection aggregates in a sort key use <paramref name="mapping"/>.</summary>
        /// <param name="sortDirective">Parsed sort. <see cref="SortDirective.Items"/> must contain at least one key. Nested <c>sortfor</c> keys are ignored.</param>
        /// <param name="mapping">Scalar columns and nested collection SQL.</param>
        /// <param name="paramNamePrefix">Bind-name prefix. Must start with a letter and contain only letters, digits, and underscores.</param>
        /// <returns>The <c>ORDER BY</c> text without the <c>ORDER BY</c> keyword, and the bound parameter values keyed by bind name.</returns>
        (string orderByClause, Dictionary<string, object> parameters) RenderOrderByClause(SortDirective sortDirective, SqlQueryMapping mapping, string paramNamePrefix);
    }
}
