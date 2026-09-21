using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using System.Text;
using System.Text.RegularExpressions;

namespace Expresso.Rendering
{
    /// <summary>
    /// Portable IR walker. SQL Server quoting and function spellings are the defaults;
    /// dialect packages override hooks for identifiers, bind names, and function SQL.
    /// </summary>
    public abstract partial class ExpressionToSqlQueryClauseTransformerBase : IExpressionToQueryClauseTransformer
    {
        private static readonly Regex PrefixPattern = new(@"^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);

        /// <summary>
        /// Converts a filter criteria to an expression for the SQL WHERE clause.
        /// </summary>
        public (string whereClause, Dictionary<string, object> parameters) RenderWhereClause(
            FilterCriteria filterCriteria,
            Dictionary<string, string> fieldToColumnMap,
            string paramNamePrefix)
        {
            if (fieldToColumnMap is null)
            {
                throw new ArgumentNullException(nameof(fieldToColumnMap));
            }

            return RenderWhereClause(filterCriteria, new SqlQueryMapping(fieldToColumnMap), paramNamePrefix);
        }

        /// <summary>
        /// Converts a sort order directive to an expression for the SQL ORDER BY clause.
        /// </summary>
        public (string orderByClause, Dictionary<string, object> parameters) RenderOrderByClause(
            SortDirective sortDirective,
            Dictionary<string, string> fieldToColumnMap,
            string paramNamePrefix)
        {
            if (fieldToColumnMap is null)
            {
                throw new ArgumentNullException(nameof(fieldToColumnMap));
            }

            return RenderOrderByClause(sortDirective, new SqlQueryMapping(fieldToColumnMap), paramNamePrefix);
        }

        /// <summary>Quotes a possibly dotted identifier (table.column).</summary>
        protected virtual string QuoteIdentifier(string input)
        {
            var parts = input.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(".", parts.Select(QuoteIdentifierPart));
        }

        /// <summary>Quotes a single identifier part. Default is SQL Server brackets.</summary>
        protected virtual string QuoteIdentifierPart(string part) => $"[{part}]";

        /// <summary>Formats a positional bind name. Default is <c>@prefix_n</c>.</summary>
        protected virtual string FormatParameterName(string prefix, int index) => $"@{prefix}_{index}";

        /// <summary>Operator used to concatenate string SQL fragments (LIKE patterns). Default is <c> + </c>.</summary>
        protected virtual string SqlStringConcatOperator => " + ";

        protected virtual void GenerateClause(
            AbstractExpression expression,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            switch (expression)
            {
                case AndFunc andFunc:
                    GenerateAndClause(andFunc, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    break;
                case OrFunc orFunc:
                    GenerateOrClause(orFunc, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    break;
                case NotFunc notFunc:
                    GenerateNotClause(notFunc, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    break;
                case EqFunc eqFunc:
                    GenerateComparisonClause(eqFunc, "=", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    break;
                case NeqFunc neqFunc:
                    GenerateComparisonClause(neqFunc, "!=", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    break;
                case GtFunc gtFunc:
                    GenerateComparisonClause(gtFunc, ">", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    break;
                case GteFunc gteFunc:
                    GenerateComparisonClause(gteFunc, ">=", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    break;
                case LtFunc ltFunc:
                    GenerateComparisonClause(ltFunc, "<", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    break;
                case LteFunc lteFunc:
                    GenerateComparisonClause(lteFunc, "<=", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    break;
                case AbsFunc absFunc:
                    GenerateNamedFunction("ABS", absFunc.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    break;
                case AddFunc addFunc:
                    GenerateArithOperationClause(addFunc, "+", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    break;
                case SubFunc subFunc:
                    GenerateArithOperationClause(subFunc, "-", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    break;
                case MultFunc multFunc:
                    GenerateArithOperationClause(multFunc, "*", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    break;
                case DivFunc divFunc:
                    GenerateArithOperationClause(divFunc, "/", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    break;
                case InFunc inFunc:
                    GenerateInClause(inFunc, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    break;
                case IsNullFunc isNullFunc:
                    GenerateIsNullClause(isNullFunc, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    break;
                case Field field:
                    string mapKey = field.Name.ToLower();
                    if (!fieldToColumnMap.ContainsKey(mapKey))
                    {
                        throw new ArgumentException($"No mapping for the {field.Name} field");
                    }
                    sqlBuilder.Append(QuoteIdentifier(fieldToColumnMap[mapKey]));
                    break;
                case Literal literal:
                    sqlBuilder.Append(AddParameter(literal.Value, parameters, paramNamePrefix));
                    break;
                default:
                    if (TryGenerateCollectionFunction(expression, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections)
                        || TryGenerateStringFunction(expression, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections)
                        || TryGenerateDateTimeFunction(expression, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections)
                        || TryGenerateNumericFunction(expression, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections))
                    {
                        break;
                    }
                    throw new NotSupportedException($"Expression type '{expression.GetType().Name}' is not supported.");
            }
        }

        private void GenerateAndClause(AndFunc andFunc, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix, Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append('(');
            for (int i = 0; i < andFunc.Arguments.Count; i++)
            {
                if (i > 0)
                {
                    sqlBuilder.Append(" AND ");
                }
                GenerateClause(andFunc.Arguments[i], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            }
            sqlBuilder.Append(')');
        }

        private void GenerateOrClause(OrFunc orFunc, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix, Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append('(');
            for (int i = 0; i < orFunc.Arguments.Count; i++)
            {
                if (i > 0)
                {
                    sqlBuilder.Append(" OR ");
                }
                GenerateClause(orFunc.Arguments[i], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            }
            sqlBuilder.Append(')');
        }

        private void GenerateNotClause(NotFunc notFunc, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix, Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("NOT (");
            GenerateClause(notFunc.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(')');
        }

        private void GenerateComparisonClause(ComparisonFunction comparisonFunc, string operatorSymbol, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix, Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append('(');
            GenerateClause(comparisonFunc.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append($" {operatorSymbol} ");
            GenerateClause(comparisonFunc.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(')');
        }

        protected void GenerateArithOperationClause(NumericArithFunction func, string operatorSymbol, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix, Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append('(');
            GenerateClause(func.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append($" {operatorSymbol} ");
            GenerateClause(func.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(')');
        }

        private void GenerateInClause(InFunc inFunc, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix, Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append('(');
            GenerateClause(inFunc.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(" IN (");
            for (int i = 1; i < inFunc.Arguments.Count; i++)
            {
                if (i > 1)
                {
                    sqlBuilder.Append(", ");
                }
                GenerateClause(inFunc.Arguments[i], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            }
            sqlBuilder.Append("))");
        }

        private void GenerateIsNullClause(IsNullFunc isNullFunc, Dictionary<string, string> fieldToColumnMap, StringBuilder sqlBuilder, Dictionary<string, object> parameters, string paramNamePrefix, Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append('(');
            GenerateClause(isNullFunc.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(" IS NULL)");
        }

        protected string AddParameter(object value, Dictionary<string, object> parameters, string paramNamePrefix)
        {
            int paramNum = parameters.Count;
            var parameterName = FormatParameterName(paramNamePrefix, paramNum);
            parameters[parameterName] = value;
            return parameterName;
        }

        protected void EnsureParamNamePrefix(string paramNamePrefix)
        {
            if (paramNamePrefix is null)
            {
                throw new ArgumentNullException(nameof(paramNamePrefix));
            }
            if (!PrefixPattern.IsMatch(paramNamePrefix))
            {
                throw new ArgumentException("Incorrect prefix for sql parameter names.", nameof(paramNamePrefix));
            }
        }

        protected void GenerateNamedFunction(
            string sqlName,
            IReadOnlyList<AbstractExpression> arguments,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append(sqlName);
            sqlBuilder.Append('(');
            for (int i = 0; i < arguments.Count; i++)
            {
                if (i > 0)
                {
                    sqlBuilder.Append(", ");
                }
                GenerateClause(arguments[i], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            }
            sqlBuilder.Append(')');
        }
    }
}
