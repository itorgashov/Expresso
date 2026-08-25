using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Parsing
{
    internal sealed partial class ExpressionParser
    {
        private static bool TryCreateStringFunction(string functionName, List<AbstractExpression> arguments, out AbstractExpression result)
        {
            result = functionName.ToLowerInvariant() switch
            {
                "startswith" => CreateStartsWith(arguments),
                "endswith" => CreateEndsWith(arguments),
                "contains" => CreateContains(arguments),
                "substring" or "substr" => CreateSubstring(arguments),
                "left" => CreateLeft(arguments),
                "right" => CreateRight(arguments),
                "concat" => CreateConcat(arguments),
                "lower" => CreateLower(arguments),
                "upper" => CreateUpper(arguments),
                "trim" => CreateTrim(arguments),
                "ltrim" => CreateLTrim(arguments),
                "rtrim" => CreateRTrim(arguments),
                "len" => CreateLen(arguments),
                "replace" => CreateReplace(arguments),
                "indexof" => CreateIndexOf(arguments),
                _ => null!
            };

            return result is not null;
        }

        private static AbstractExpression CreateStartsWith(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 2, "Startswith() function should have 2 arguments.");
            return new StrStartswithFunc(CoerceToString(arguments[0]), CoerceToString(arguments[1]));
        }

        private static AbstractExpression CreateEndsWith(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 2, "Endswith() function should have 2 arguments.");
            return new StrEndswithFunc(CoerceToString(arguments[0]), CoerceToString(arguments[1]));
        }

        private static AbstractExpression CreateContains(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 2, "Contains() function should have 2 arguments.");
            return new StrContainsFunc(CoerceToString(arguments[0]), CoerceToString(arguments[1]));
        }

        private static AbstractExpression CreateSubstring(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 3, "Substring() function should have 3 arguments.");
            return new SubStringFunc(CoerceToString(arguments[0]), CoerceToInt(arguments[1]), CoerceToInt(arguments[2]));
        }

        private static AbstractExpression CreateLeft(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 2, "Left() function should have 2 arguments.");
            return new LeftFunc(CoerceToString(arguments[0]), CoerceToInt(arguments[1]));
        }

        private static AbstractExpression CreateRight(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 2, "Right() function should have 2 arguments.");
            return new RightFunc(CoerceToString(arguments[0]), CoerceToInt(arguments[1]));
        }

        private static AbstractExpression CreateConcat(List<AbstractExpression> arguments)
        {
            if (arguments.Count < 2)
            {
                throw new Exception("Concat() function should have at least 2 arguments.");
            }

            var coerced = arguments.Select(CoerceToString).ToList();
            return new ConcatFunc(coerced);
        }

        private static AbstractExpression CreateLower(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Lower() function should have 1 argument.");
            return new LowerFunc(CoerceToString(arguments[0]));
        }

        private static AbstractExpression CreateUpper(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Upper() function should have 1 argument.");
            return new UpperFunc(CoerceToString(arguments[0]));
        }

        private static AbstractExpression CreateTrim(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Trim() function should have 1 argument.");
            return new TrimFunc(CoerceToString(arguments[0]));
        }

        private static AbstractExpression CreateLTrim(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Ltrim() function should have 1 argument.");
            return new LTrimFunc(CoerceToString(arguments[0]));
        }

        private static AbstractExpression CreateRTrim(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Rtrim() function should have 1 argument.");
            return new RTrimFunc(CoerceToString(arguments[0]));
        }

        private static AbstractExpression CreateLen(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Len() function should have 1 argument.");
            return new LenFunc(CoerceToString(arguments[0]));
        }

        private static AbstractExpression CreateReplace(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 3, "Replace() function should have 3 arguments.");
            return new ReplaceFunc(CoerceToString(arguments[0]), CoerceToString(arguments[1]), CoerceToString(arguments[2]));
        }

        private static AbstractExpression CreateIndexOf(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 2, "Indexof() function should have 2 arguments.");
            return new IndexOfFunc(CoerceToString(arguments[0]), CoerceToString(arguments[1]));
        }

        private static void RequireCount(List<AbstractExpression> arguments, int expected, string message)
        {
            if (arguments.Count != expected)
            {
                throw new Exception(message);
            }
        }

        private static AbstractExpression CoerceToString(AbstractExpression expression)
        {
            return expression is StringLiteral stringLiteral
                ? CreateLiteral(stringLiteral.Value, typeof(string))
                : expression;
        }

        private static AbstractExpression CoerceToInt(AbstractExpression expression)
        {
            return expression is StringLiteral stringLiteral
                ? CreateLiteral(stringLiteral.Value, typeof(int))
                : expression;
        }
    }
}
