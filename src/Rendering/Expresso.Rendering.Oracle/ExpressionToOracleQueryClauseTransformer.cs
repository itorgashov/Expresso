using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using System.Text;

namespace Expresso.Rendering
{
    public sealed class ExpressionToOracleQueryClauseTransformer : AnsiSqlQueryClauseTransformerBase
    {
        protected override string FormatParameterName(string prefix, int index) => $":{prefix}_{index}";

        protected override string SubstringFunctionName => "SUBSTR";

        protected override bool UseConcatFunction => false;

        protected override bool UsePercentModulo => false;

        protected override string CeilingFunctionName => "CEIL";

        protected override void AppendLeft(
            LeftFunc left,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("SUBSTR(");
            GenerateClause(left.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(", 1, ");
            GenerateClause(left.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(')');
        }

        protected override void AppendRight(
            RightFunc right,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("SUBSTR(");
            GenerateClause(right.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(", -(");
            GenerateClause(right.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append("))");
        }

        protected override void AppendIndexOf(
            IndexOfFunc indexOf,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("(INSTR(");
            GenerateClause(indexOf.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(", ");
            GenerateClause(indexOf.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(") - 1)");
        }

        protected override void AppendYear(
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            AppendExtract("YEAR", argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
        }

        protected override void AppendMonth(
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            AppendExtract("MONTH", argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
        }

        protected override void AppendDay(
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            AppendExtract("DAY", argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
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
            switch (part)
            {
                case "dayofyear":
                    sqlBuilder.Append("TO_NUMBER(TO_CHAR(");
                    GenerateClause(argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    sqlBuilder.Append(", 'DDD'))");
                    return;
                case "hour":
                    AppendExtract("HOUR", argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return;
                case "minute":
                    AppendExtract("MINUTE", argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return;
                case "second":
                    AppendExtract("SECOND", argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return;
                default:
                    AppendExtract(part.ToUpperInvariant(), argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return;
            }
        }

        protected override void AppendDayOfWeek(
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("(TO_NUMBER(TO_CHAR(");
            GenerateClause(argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(", 'D')) - 1)");
        }

        protected override void AppendDateCast(
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("TRUNC(");
            GenerateClause(argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(')');
        }

        protected override void AppendTimeCast(
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("(");
            GenerateClause(argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(" - TRUNC(");
            GenerateClause(argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append("))");
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
            sqlBuilder.Append('(');
            GenerateClause(addFunction.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(" + ");
            if (datePart is "year" or "month")
            {
                sqlBuilder.Append("NUMTOYMINTERVAL(");
                GenerateClause(addFunction.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                sqlBuilder.Append(", '");
                sqlBuilder.Append(datePart.ToUpperInvariant());
                sqlBuilder.Append("'))");
            }
            else
            {
                string unit = datePart.ToUpperInvariant();
                sqlBuilder.Append("NUMTODSINTERVAL(");
                GenerateClause(addFunction.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                sqlBuilder.Append(", '");
                sqlBuilder.Append(unit);
                sqlBuilder.Append("'))");
            }
        }

        private void AppendExtract(
            string field,
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("EXTRACT(");
            sqlBuilder.Append(field);
            sqlBuilder.Append(" FROM ");
            GenerateClause(argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(')');
        }
    }
}
