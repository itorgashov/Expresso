using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using System.Text;

namespace Expresso.SqlServer
{
    public partial class ExpressionToSqlServerQueryClauseTransformer
    {
        private enum LikePatternKind
        {
            Prefix,
            Suffix,
            Contains
        }

        private bool TryGenerateStringFunction(
            AbstractExpression expression,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            switch (expression)
            {
                case StrStartswithFunc startsWith:
                    GenerateLikeClause(startsWith.Arguments[0], startsWith.Arguments[1], LikePatternKind.Prefix, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case StrEndswithFunc endsWith:
                    GenerateLikeClause(endsWith.Arguments[0], endsWith.Arguments[1], LikePatternKind.Suffix, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case StrContainsFunc contains:
                    GenerateLikeClause(contains.Arguments[0], contains.Arguments[1], LikePatternKind.Contains, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case SubStringFunc substring:
                    GenerateNamedFunction("SUBSTRING", substring.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case LeftFunc left:
                    GenerateNamedFunction("LEFT", left.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case RightFunc right:
                    GenerateNamedFunction("RIGHT", right.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case ConcatFunc concat:
                    GenerateNamedFunction("CONCAT", concat.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case LowerFunc lower:
                    GenerateNamedFunction("LOWER", lower.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case UpperFunc upper:
                    GenerateNamedFunction("UPPER", upper.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case TrimFunc trim:
                    GenerateNamedFunction("TRIM", trim.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case LTrimFunc ltrim:
                    GenerateNamedFunction("LTRIM", ltrim.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case RTrimFunc rtrim:
                    GenerateNamedFunction("RTRIM", rtrim.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case LenFunc len:
                    GenerateNamedFunction("LEN", len.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case ReplaceFunc replace:
                    GenerateNamedFunction("REPLACE", replace.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case IndexOfFunc indexOf:
                    GenerateIndexOfClause(indexOf, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                default:
                    return false;
            }
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
                sqlBuilder.Append('(');
                if (kind is LikePatternKind.Suffix or LikePatternKind.Contains)
                {
                    sqlBuilder.Append("'%' + ");
                }

                sqlBuilder.Append("REPLACE(REPLACE(REPLACE(");
                GenerateClause(patternExpr, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                sqlBuilder.Append(", '\\', '\\\\'), '%', '\\%'), '_', '\\_')");

                if (kind is LikePatternKind.Prefix or LikePatternKind.Contains)
                {
                    sqlBuilder.Append(" + '%'");
                }

                sqlBuilder.Append(')');
            }

            sqlBuilder.Append(" ESCAPE '\\')");
        }

        private static string BuildLikePattern(string escapedLiteral, LikePatternKind kind) =>
            kind switch
            {
                LikePatternKind.Prefix => escapedLiteral + "%",
                LikePatternKind.Suffix => "%" + escapedLiteral,
                LikePatternKind.Contains => "%" + escapedLiteral + "%",
                _ => escapedLiteral
            };

        private static string EscapeLikeLiteral(string value) =>
            value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

        private void GenerateNamedFunction(
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

        private void GenerateIndexOfClause(
            IndexOfFunc indexOf,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("(ISNULL(NULLIF(CHARINDEX(");
            GenerateClause(indexOf.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(", ");
            GenerateClause(indexOf.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append("), 0), 0) - 1)");
        }
    }
}
