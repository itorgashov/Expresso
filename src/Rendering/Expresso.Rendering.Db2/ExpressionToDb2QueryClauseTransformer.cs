using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using System.Text;

namespace Expresso.Rendering
{
    /// <summary>IBM Db2 renderer. Double-quoted identifiers, <c>@</c> parameters, and Db2 function spellings.</summary>
    public sealed class ExpressionToDb2QueryClauseTransformer : AnsiSqlQueryClauseTransformerBase
    {
        protected override string SubstringFunctionName => "SUBSTR";

        protected override bool UseConcatFunction => false;

        protected override bool UsePercentModulo => false;

        protected override void AppendIndexOf(
            IndexOfFunc indexOf,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("(LOCATE(");
            GenerateClause(indexOf.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(", ");
            GenerateClause(indexOf.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(") - 1)");
        }

        protected override void AppendDatePart(
            string part,
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            string fn = part switch
            {
                "dayofyear" => "DAYOFYEAR",
                "hour" => "HOUR",
                "minute" => "MINUTE",
                "second" => "SECOND",
                _ => part.ToUpperInvariant()
            };
            sqlBuilder.Append(fn);
            sqlBuilder.Append('(');
            GenerateClause(argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(')');
        }

        protected override void AppendDayOfWeek(
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("(DAYOFWEEK(");
            GenerateClause(argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(") - 1)");
        }

        protected override void AppendDateAdd(
            string datePart,
            DateTimeAddFunction addFunction,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            string unit = datePart.ToUpperInvariant() + "S";
            sqlBuilder.Append('(');
            GenerateClause(addFunction.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(" + ");
            GenerateClause(addFunction.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(' ');
            sqlBuilder.Append(unit);
            sqlBuilder.Append(')');
        }
    }
}
