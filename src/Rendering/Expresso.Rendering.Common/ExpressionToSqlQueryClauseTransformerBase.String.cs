using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using System.Text;

namespace Expresso.Rendering
{
    public abstract partial class ExpressionToSqlQueryClauseTransformerBase
    {
        protected enum LikePatternKind
        {
            Prefix,
            Suffix,
            Contains
        }

        /// <summary>SQL function for <c>len</c>. Default is SQL Server <c>LEN</c>.</summary>
        protected virtual string LengthFunctionName => "LEN";

        /// <summary>SQL function for <c>substring</c>. Default is <c>SUBSTRING</c>.</summary>
        protected virtual string SubstringFunctionName => "SUBSTRING";

        /// <summary>When true, <c>concat</c> uses a function named <see cref="ConcatFunctionName"/>; otherwise SQL <c>||</c>.</summary>
        protected virtual bool UseConcatFunction => true;

        /// <summary>Name of the SQL CONCAT function when <see cref="UseConcatFunction"/> is true.</summary>
        protected virtual string ConcatFunctionName => "CONCAT";

        protected virtual void AppendSubstring(
            SubStringFunc substring,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            GenerateNamedFunction(SubstringFunctionName, substring.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
        }

        protected virtual void AppendLeft(
            LeftFunc left,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            GenerateNamedFunction("LEFT", left.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
        }

        protected virtual void AppendRight(
            RightFunc right,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            GenerateNamedFunction("RIGHT", right.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
        }

        protected virtual void AppendConcat(
            ConcatFunc concat,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            if (UseConcatFunction)
            {
                GenerateNamedFunction(ConcatFunctionName, concat.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                return;
            }

            sqlBuilder.Append('(');
            for (int i = 0; i < concat.Arguments.Count; i++)
            {
                if (i > 0)
                {
                    sqlBuilder.Append(SqlStringConcatOperator);
                }
                GenerateClause(concat.Arguments[i], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            }
            sqlBuilder.Append(')');
        }

        /// <summary>
        /// Zero-based <c>indexof</c>, <c>-1</c> if missing, <c>0</c> for an empty <c>find</c>, NULL if either argument is NULL.
        /// Default is SQL Server: <c>CHARINDEX</c> returns 0 for an empty <c>find</c>, and <c>= ''</c> / <c>LEN</c> ignore
        /// trailing spaces, so emptiness is tested with <c>DATALENGTH</c>.
        /// </summary>
        protected virtual void AppendIndexOf(
            IndexOfFunc indexOf,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("(CASE WHEN DATALENGTH(");
            GenerateClause(indexOf.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(") = 0 AND ");
            GenerateClause(indexOf.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(" IS NOT NULL THEN 0 ELSE CHARINDEX(");
            GenerateClause(indexOf.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(", ");
            GenerateClause(indexOf.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(") - 1 END)");
        }

        private void GenerateLikeClause(
            AbstractExpression source,
            AbstractExpression patternExpr,
            LikePatternKind kind,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append('(');
            GenerateClause(source, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(" LIKE ");

            if (patternExpr is Literal { Value: string raw })
            {
                sqlBuilder.Append(AddParameter(BuildLikePattern(EscapeLikeLiteral(raw), kind), parameters, paramNamePrefix));
            }
            else
            {
                AppendLikeExpressionPattern(
                    kind,
                    () => AppendLikeEscapedExpression(
                        () => GenerateClause(patternExpr, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections),
                        sqlBuilder),
                    sqlBuilder);
            }

            AppendLikeEscapeClause(sqlBuilder);
        }

        /// <summary>Closes a LIKE predicate with an ESCAPE clause. Default is SQL Server <c>ESCAPE '\'</c>.</summary>
        protected virtual void AppendLikeEscapeClause(StringBuilder sqlBuilder) =>
            sqlBuilder.Append(" ESCAPE '\\')");

        /// <summary>
        /// Escapes <c>\</c>, <c>%</c>, and <c>_</c> in a non-literal LIKE pattern.
        /// Default uses SQL Server string-literal backslash encoding.
        /// </summary>
        protected virtual void AppendLikeEscapedExpression(Action emitInner, StringBuilder sqlBuilder)
        {
            sqlBuilder.Append("REPLACE(REPLACE(REPLACE(");
            emitInner();
            sqlBuilder.Append(", '\\', '\\\\'), '%', '\\%'), '_', '\\_')");
        }

        /// <summary>
        /// Concatenates optional <c>%</c> wildcards around an already-escaped LIKE pattern expression.
        /// Default uses the dialect string-concat operator (SQL Server <c>+</c>).
        /// </summary>
        protected virtual void AppendLikeExpressionPattern(
            LikePatternKind kind,
            Action emitEscapedPattern,
            StringBuilder sqlBuilder)
        {
            bool prefix = kind is LikePatternKind.Suffix or LikePatternKind.Contains;
            bool suffix = kind is LikePatternKind.Prefix or LikePatternKind.Contains;
            sqlBuilder.Append('(');
            if (prefix)
            {
                sqlBuilder.Append("'%'");
                sqlBuilder.Append(SqlStringConcatOperator);
            }

            emitEscapedPattern();
            if (suffix)
            {
                sqlBuilder.Append(SqlStringConcatOperator);
                sqlBuilder.Append("'%'");
            }

            sqlBuilder.Append(')');
        }

        private static string BuildLikePattern(string escapedLiteral, LikePatternKind kind) =>
            kind switch
            {
                LikePatternKind.Prefix => escapedLiteral + "%",
                LikePatternKind.Suffix => "%" + escapedLiteral,
                LikePatternKind.Contains => "%" + escapedLiteral + "%",
                _ => escapedLiteral
            };

        /// <summary>Escapes <c>\</c>, then <c>%</c>, then <c>_</c>. SQL Server also escapes <c>[</c>.</summary>
        protected virtual string EscapeLikeLiteral(string value) =>
            value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
    }
}
