using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using System.Text;

namespace Expresso.Rendering
{
    /// <summary>PostgreSQL renderer. Double-quoted identifiers, <c>@</c> parameters, and PostgreSQL function spellings.</summary>
    public sealed class ExpressionToPostgreSqlQueryClauseTransformer : AnsiSqlQueryClauseTransformerBase
    {
        protected override string SubstringFunctionName => "SUBSTR";

        protected override bool UseConcatFunction => true;

        protected override void AppendIndexOf(
            IndexOfFunc indexOf,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("(STRPOS(");
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
            string extract = part switch
            {
                "dayofyear" => "DOY",
                "hour" => "HOUR",
                "minute" => "MINUTE",
                "second" => "SECOND",
                _ => part.ToUpperInvariant()
            };
            AppendExtract(extract, argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
        }

        protected override void AppendDayOfWeek(
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            AppendExtract("DOW", argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
        }

        protected override void AppendRound(
            RoundFunc roundFunc,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            // PostgreSQL has round(double precision) and round(numeric, int), but not round(double precision, int).
            sqlBuilder.Append("ROUND(CAST(");
            GenerateClause(roundFunc.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(" AS numeric), ");
            AppendRoundDigits(roundFunc, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(')');
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
            string interval = datePart switch
            {
                "year" => "1 year",
                "month" => "1 month",
                "day" => "1 day",
                "hour" => "1 hour",
                "minute" => "1 minute",
                "second" => "1 second",
                _ => "1 " + datePart
            };
            sqlBuilder.Append('(');
            GenerateClause(addFunction.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(" + ((");
            GenerateClause(addFunction.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(") * INTERVAL '");
            sqlBuilder.Append(interval);
            sqlBuilder.Append("'))");
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
            sqlBuilder.Append("CAST(EXTRACT(");
            sqlBuilder.Append(field);
            sqlBuilder.Append(" FROM ");
            GenerateClause(argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(") AS integer)");
        }
    }
}
