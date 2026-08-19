using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using System.Text;
using System.Text.RegularExpressions;

namespace Expresso.SqlServer
{
    public class ExpressionToSqlServerQueryClauseTransformer : IExpressionToQueryClauseTransformer
    {
        private const string _prefixPattern = @"^[A-Za-z][A-Za-z0-9_]*$";

        /// <summary>
        /// Converts a filter criteria to an expression for the SQL WHERE clause.
        /// </summary>
        /// <param name="filterCriteria">The FilterCriteria object to convert.</param>
        /// <param name="fieldToColumnMap">Dictionary that maps field names in the filter criteria to table columns in the database.</param>
        /// <param name="paramNamePrefix">Prefix for names of positional SQL query parameters.</param>
        /// <returns>A tuple containing the expression for SQL WHERE clause and a dictionary of parameters that maps parameter names to values.</returns>
        public (string whereClause, Dictionary<string, object> parameters) RenderWhereClause(FilterCriteria filterCriteria, Dictionary<string, string> fieldToColumnMap, string paramNamePrefix)
        {
            StringBuilder sqlBuilder = new StringBuilder();
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            if (filterCriteria is null)
            {
                throw new ArgumentNullException(nameof(filterCriteria));
            }
            if (filterCriteria.Expression is null)
            {
                throw new ArgumentException("The expression of the filter criteria is null.", nameof(filterCriteria));
            }
            if (fieldToColumnMap is null)
            {
                throw new ArgumentNullException(nameof(fieldToColumnMap));
            }
            if (paramNamePrefix is null)
            {
                throw new ArgumentNullException(nameof(paramNamePrefix));
            }
            if (!new Regex(_prefixPattern).IsMatch(paramNamePrefix))
            {
                throw new ArgumentException("Incorrect prefix for sql parameter names.", nameof(paramNamePrefix));
            }

            GenerateClause(filterCriteria.Expression, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            return (sqlBuilder.ToString(), parameters);
        }

        /// <summary>
        /// Converts a sort order directive to an expression for the SQL ORDER BY clause.
        /// </summary>
        /// <param name="sortDirective">The SortDirective object to convert.</param>
        /// <param name="fieldToColumnMap">Dictionary that maps field names in the filter criteria to table columns in the database.</param>
        /// <param name="paramNamePrefix">Prefix for names of positional SQL query parameters.</param>
        /// <returns>A tuple containing the expression for SQL ORDER BY clause and a dictionary of parameters that maps parameter names to values.</returns>
        public (string orderByClause, Dictionary<string, object> parameters) RenderOrderByClause(SortDirective sortDirective, Dictionary<string, string> fieldToColumnMap, string paramNamePrefix)
        {
            if (sortDirective == null)
            {
                throw new ArgumentNullException(nameof(sortDirective));
            }
            if (sortDirective.Items == null || sortDirective.Items.Count == 0)
            {
                throw new ArgumentException("Sort directive must contain at least one item", nameof(sortDirective));
            }
            if (fieldToColumnMap == null)
            {
                throw new ArgumentNullException(nameof(fieldToColumnMap));
            }
            if (paramNamePrefix is null)
            {
                throw new ArgumentNullException(nameof(paramNamePrefix));
            }
            if (!new Regex(_prefixPattern).IsMatch(paramNamePrefix))
            {
                throw new ArgumentException("Incorrect prefix for sql parameter names.", nameof(paramNamePrefix));
            }

            StringBuilder sqlBuilder = new StringBuilder();
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            for (int i = 0; i < sortDirective.Items.Count; i++)
            {
                var item = sortDirective.Items[i];

                if (i > 0)
                {
                    sqlBuilder.Append(", ");
                }

                if (item.Expression is BooleanFunction booleanExpression)
                {
                    sqlBuilder.Append("(CASE WHEN ");
                    GenerateClause(booleanExpression, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    sqlBuilder.Append(" THEN 1 ELSE 0 END)");
                }
                else
                {
                    GenerateClause(item.Expression, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                }

                sqlBuilder.Append(item.Direction == SortDirection.Ascending ? " ASC" : " DESC");
            }

            return (sqlBuilder.ToString(), parameters);
        }

        private void GenerateClause(AbstractExpression expression, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix)
        {
            switch (expression)
            {
                case AndFunc andFunc:
                    GenerateAndClause(andFunc, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case OrFunc orFunc:
                    GenerateOrClause(orFunc, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case NotFunc notFunc:
                    GenerateNotClause(notFunc, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case EqFunc eqFunc:
                    GenerateComparisonClause(eqFunc, "=", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case NeqFunc neqFunc:
                    GenerateComparisonClause(neqFunc, "!=", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case GtFunc gtFunc:
                    GenerateComparisonClause(gtFunc, ">", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case GteFunc gteFunc:
                    GenerateComparisonClause(gteFunc, ">=", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case LtFunc ltFunc:
                    GenerateComparisonClause(ltFunc, "<", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case LteFunc lteFunc:
                    GenerateComparisonClause(lteFunc, "<=", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case AbsFunc absFunc:
                    GenerateSingleArgFunctionClause(absFunc, "ABS", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case AddFunc addFunc:
                    GenerateArithOperationClause(addFunc, "+", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case SubFunc subFunc:
                    GenerateArithOperationClause(subFunc, "-", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case MultFunc multFunc:
                    GenerateArithOperationClause(multFunc, "*", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case DivFunc divFunc:
                    GenerateArithOperationClause(divFunc, "/", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case InFunc inFunc:
                    GenerateInClause(inFunc, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case StrStartswithFunc startsWithFunc:
                    GenerateStartsWithClause(startsWithFunc, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case SubStringFunc substringFunc:
                    GenerateSubstringClause(substringFunc, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case IsNullFunc isNullFunc:
                    GenerateIsNullClause(isNullFunc, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    break;
                case Field field:
                    string mapKey = field.Name.ToLower();
                    if (!fieldToColumnMap.ContainsKey(mapKey))
                    {
                        throw new ArgumentException($"No mapping for the {field.Name} field");
                    }
                    sqlBuilder.Append(Bracketize(fieldToColumnMap[mapKey]));
                    break;
                case Literal literal:
                    sqlBuilder.Append(AddParameter(literal.Value, parameters, paramNamePrefix));
                    break;
                default:
                    throw new NotSupportedException($"Expression type '{expression.GetType().Name}' is not supported.");
            }
        }

        private static string Bracketize(string input)
        {
            //if (string.IsNullOrWhiteSpace(input))
            //    throw new ArgumentException("Input cannot be null or empty.", nameof(input));

            var parts = input.Split('.', StringSplitOptions.RemoveEmptyEntries);

            return string.Join(".", parts.Select(p => $"[{p}]"));
        }

        private void GenerateAndClause(AndFunc andFunc, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix)
        {
            sqlBuilder.Append('(');
            for (int i = 0; i < andFunc.Arguments.Count; i++)
            {
                if (i > 0)
                {
                    sqlBuilder.Append(" AND ");
                }
                GenerateClause(andFunc.Arguments[i], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            }
            sqlBuilder.Append(')');
        }

        private void GenerateOrClause(OrFunc orFunc, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix)
        {
            sqlBuilder.Append('(');
            for (int i = 0; i < orFunc.Arguments.Count; i++)
            {
                if (i > 0)
                {
                    sqlBuilder.Append(" OR ");
                }
                GenerateClause(orFunc.Arguments[i], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            }
            sqlBuilder.Append(')');
        }

        private void GenerateNotClause(NotFunc notFunc, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix)
        {
            sqlBuilder.Append("NOT (");
            GenerateClause(notFunc.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(')');
        }

        private void GenerateComparisonClause(ComparisonFunction comparisonFunc, string operatorSymbol, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix)
        {
            sqlBuilder.Append('(');
            GenerateClause(comparisonFunc.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append($" {operatorSymbol} ");
            GenerateClause(comparisonFunc.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(')');
        }

        private void GenerateSingleArgFunctionClause(NumericSingleArgFunction func, string funcSymbol, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix)
        {
            sqlBuilder.Append(funcSymbol);
            sqlBuilder.Append('(');
            GenerateClause(func.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(')');
        }

        private void GenerateArithOperationClause(NumericArithFunction func, string operatorSymbol, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix)
        {
            sqlBuilder.Append('(');
            GenerateClause(func.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append($" {operatorSymbol} ");
            GenerateClause(func.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(')');
        }

        private void GenerateInClause(InFunc inFunc, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix)
        {
            sqlBuilder.Append('(');
            GenerateClause(inFunc.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(" IN (");
            for (int i = 1; i < inFunc.Arguments.Count; i++)
            {
                if (i > 1)
                {
                    sqlBuilder.Append(", ");
                }
                GenerateClause(inFunc.Arguments[i], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            }
            sqlBuilder.Append("))");
        }

        private void GenerateStartsWithClause(StrStartswithFunc startsWithFunc, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix)
        {
            sqlBuilder.Append('(');
            GenerateClause(startsWithFunc.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(" LIKE ");
            GenerateClause(startsWithFunc.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(" + '%')");
        }

        private void GenerateSubstringClause(SubStringFunc substringFunc, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix)
        {
            sqlBuilder.Append("SUBSTRING(");
            GenerateClause(substringFunc.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(", ");
            GenerateClause(substringFunc.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(", ");
            GenerateClause(substringFunc.Arguments[2], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(')');
        }

        private void GenerateIsNullClause(IsNullFunc isNullFunc, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix)
        {
            sqlBuilder.Append('(');
            GenerateClause(isNullFunc.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(" IS NULL)");
        }

        private static string AddParameter(object value, Dictionary<string, object> parameters, string paramNamePrefix)
        {
            int paramNum = parameters.Count;
            var parameterName = $"@{paramNamePrefix}_{paramNum}";
            parameters[parameterName] = value;
            return parameterName;
        }
    }
}
