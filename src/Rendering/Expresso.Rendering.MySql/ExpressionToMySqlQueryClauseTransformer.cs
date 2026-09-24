using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using System.Text;

namespace Expresso.Rendering
{
    /// <summary>MySQL and MariaDB renderer. Backtick identifiers, <c>@</c> parameters, and MySQL function spellings.</summary>
    public sealed class ExpressionToMySqlQueryClauseTransformer : ExpressionToSqlQueryClauseTransformerBase
    {
        protected override string QuoteIdentifierPart(string part) =>
            "`" + part.Replace("`", "``") + "`";

        protected override string LengthFunctionName => "CHAR_LENGTH";

        protected override string SqlStringConcatOperator => ", ";

        /// <summary>MySQL treats <c>\</c> as a string escape, so <c>ESCAPE '\'</c> is invalid SQL.</summary>
        protected override void AppendLikeEscapeClause(StringBuilder sqlBuilder) =>
            sqlBuilder.Append(" ESCAPE '\\\\')");

        protected override void AppendLikeEscapedExpression(Action emitInner, StringBuilder sqlBuilder)
        {
            sqlBuilder.Append("REPLACE(REPLACE(REPLACE(");
            emitInner();
            sqlBuilder.Append(", '\\\\', '\\\\\\\\'), '%', '\\\\%'), '_', '\\\\_')");
        }

        protected override void AppendLikeExpressionPattern(
            LikePatternKind kind,
            Action emitEscapedPattern,
            StringBuilder sqlBuilder)
        {
            bool prefix = kind is LikePatternKind.Suffix or LikePatternKind.Contains;
            bool suffix = kind is LikePatternKind.Prefix or LikePatternKind.Contains;
            sqlBuilder.Append("CONCAT(");
            if (prefix)
            {
                sqlBuilder.Append("'%', ");
            }

            emitEscapedPattern();
            if (suffix)
            {
                sqlBuilder.Append(", '%'");
            }

            sqlBuilder.Append(')');
        }

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
            string unit = datePart.ToUpperInvariant();
            sqlBuilder.Append("DATE_ADD(");
            GenerateClause(addFunction.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(", INTERVAL ");
            GenerateClause(addFunction.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(' ');
            sqlBuilder.Append(unit);
            sqlBuilder.Append(')');
        }
    }
}
