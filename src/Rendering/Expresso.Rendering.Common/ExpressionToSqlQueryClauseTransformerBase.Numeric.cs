using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using System.Text;

namespace Expresso.Rendering
{
    public abstract partial class ExpressionToSqlQueryClauseTransformerBase
    {
        /// <summary>SQL function for <c>ceiling</c>. Default is <c>CEILING</c>.</summary>
        protected virtual string CeilingFunctionName => "CEILING";

        /// <summary>When true, <c>mod</c> uses the <c>%</c> operator; otherwise <c>MOD(a, b)</c>.</summary>
        protected virtual bool UsePercentModulo => true;

        protected virtual bool TryGenerateNumericFunction(
            AbstractExpression expression,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            switch (expression)
            {
                case ModFunc modFunc:
                    AppendMod(modFunc, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case FloorFunc floor:
                    GenerateNamedFunction("FLOOR", floor.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case CeilingFunc ceiling:
                    GenerateNamedFunction(CeilingFunctionName, ceiling.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case SqrtFunc sqrt:
                    GenerateNamedFunction("SQRT", sqrt.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case SignFunc sign:
                    GenerateNamedFunction("SIGN", sign.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case PowerFunc power:
                    GenerateNamedFunction("POWER", power.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case RoundFunc round:
                    AppendRound(round, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case MinFunc min:
                    GenerateMinMaxClause(min.Arguments[0], min.Arguments[1], "<", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case MaxFunc max:
                    GenerateMinMaxClause(max.Arguments[0], max.Arguments[1], ">", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                default:
                    return false;
            }
        }

        protected virtual void AppendMod(
            ModFunc modFunc,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            if (UsePercentModulo)
            {
                GenerateArithOperationClause(modFunc, "%", fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                return;
            }

            GenerateNamedFunction("MOD", modFunc.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
        }

        /// <summary>Default is <c>ROUND(x, digits)</c> with digits <c>0</c> when omitted (SQL Server).</summary>
        protected virtual void AppendRound(
            RoundFunc roundFunc,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("ROUND(");
            GenerateClause(roundFunc.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(", ");
            AppendRoundDigits(roundFunc, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(')');
        }

        protected void AppendRoundDigits(
            RoundFunc roundFunc,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            if (roundFunc.Arguments.Count == 2)
            {
                GenerateClause(roundFunc.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            }
            else
            {
                sqlBuilder.Append('0');
            }
        }

        private void GenerateMinMaxClause(
            AbstractExpression left,
            AbstractExpression right,
            string comparisonOperator,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("(CASE WHEN ");
            GenerateClause(left, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append($" {comparisonOperator} ");
            GenerateClause(right, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(" THEN ");
            GenerateClause(left, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(" ELSE ");
            GenerateClause(right, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(" END)");
        }
    }
}
