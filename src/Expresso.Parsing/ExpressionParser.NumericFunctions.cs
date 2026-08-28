using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Parsing
{
    internal sealed partial class ExpressionParser
    {
        private bool TryCreateNumericFunction(string functionName, List<AbstractExpression> arguments, out AbstractExpression result)
        {
            result = functionName.ToLowerInvariant() switch
            {
                "mod" => CreateMod(arguments),
                "floor" => CreateFloor(arguments),
                "ceiling" or "ceil" => CreateCeiling(arguments),
                "round" => CreateRound(arguments),
                "sign" => CreateSign(arguments),
                "power" or "pow" => CreatePower(arguments),
                "sqrt" => CreateSqrt(arguments),
                "min" => CreateMin(arguments),
                "max" => CreateMax(arguments),
                _ => null!
            };

            return result is not null;
        }

        private AbstractExpression CreateMod(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 2, "Mod() function should have 2 arguments.");
            return CreateNumericArithFunction<ModFunc>(arguments);
        }

        private AbstractExpression CreateFloor(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Floor() function should have 1 argument.");
            return CreateNumericSingleArgFunction<FloorFunc>(arguments);
        }

        private AbstractExpression CreateCeiling(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Ceiling() function should have 1 argument.");
            return CreateNumericSingleArgFunction<CeilingFunc>(arguments);
        }

        private AbstractExpression CreateSqrt(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Sqrt() function should have 1 argument.");
            return CreateNumericSingleArgFunction<SqrtFunc>(arguments);
        }

        private AbstractExpression CreateSign(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Sign() function should have 1 argument.");
            return CreateNumericSingleArgFunction<SignFunc>(arguments);
        }

        private AbstractExpression CreatePower(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 2, "Power() function should have 2 arguments.");
            return CreateNumericArithFunction<PowerFunc>(arguments);
        }

        private AbstractExpression CreateMin(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 2, "Min() function should have 2 arguments.");
            return CreateNumericArithFunction<MinFunc>(arguments);
        }

        private AbstractExpression CreateMax(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 2, "Max() function should have 2 arguments.");
            return CreateNumericArithFunction<MaxFunc>(arguments);
        }

        private AbstractExpression CreateRound(List<AbstractExpression> arguments)
        {
            if (arguments.Count is not 1 and not 2)
            {
                throw new Exception("Round() function should have 1 or 2 arguments.");
            }

            var value = CoerceToNumeric(arguments[0]);
            return arguments.Count == 1
                ? new RoundFunc(value)
                : new RoundFunc(value, CoerceToInt(arguments[1]));
        }

        private AbstractExpression CoerceToNumeric(AbstractExpression expression)
        {
            return expression is StringLiteral stringLiteral
                ? CreateLiteral(stringLiteral.Value, GetLiteralType(stringLiteral.Value))
                : expression;
        }
    }
}
