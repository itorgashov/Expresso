using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using System.Text;

namespace Expresso.SqlServer
{
    public partial class ExpressionToSqlServerQueryClauseTransformer
    {
        private bool TryGenerateNumericFunction(
            AbstractExpression expression,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix)
        {
            switch (expression)
            {
                case ModFunc modFunc:
                    GenerateArithOperationClause(modFunc, "%", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case FloorFunc floor:
                    GenerateNamedFunction("FLOOR", floor.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case CeilingFunc ceiling:
                    GenerateNamedFunction("CEILING", ceiling.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case SqrtFunc sqrt:
                    GenerateNamedFunction("SQRT", sqrt.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case SignFunc sign:
                    GenerateNamedFunction("SIGN", sign.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case PowerFunc power:
                    GenerateNamedFunction("POWER", power.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case RoundFunc round:
                    GenerateRoundClause(round, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case MinFunc min:
                    GenerateMinMaxClause(min.Arguments[0], min.Arguments[1], "<", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case MaxFunc max:
                    GenerateMinMaxClause(max.Arguments[0], max.Arguments[1], ">", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                default:
                    return false;
            }
        }

        private void GenerateRoundClause(
            RoundFunc roundFunc,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix)
        {
            sqlBuilder.Append("ROUND(");
            GenerateClause(roundFunc.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(", ");
            if (roundFunc.Arguments.Count == 2)
            {
                GenerateClause(roundFunc.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            }
            else
            {
                sqlBuilder.Append('0');
            }
            sqlBuilder.Append(')');
        }

        private void GenerateMinMaxClause(
            AbstractExpression left,
            AbstractExpression right,
            string comparisonOperator,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix)
        {
            sqlBuilder.Append("(CASE WHEN ");
            GenerateClause(left, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append($" {comparisonOperator} ");
            GenerateClause(right, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(" THEN ");
            GenerateClause(left, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(" ELSE ");
            GenerateClause(right, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(" END)");
        }
    }
}
