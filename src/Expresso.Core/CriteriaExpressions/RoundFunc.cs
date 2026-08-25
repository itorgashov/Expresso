using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class RoundFunc : AbstractFunction
    {
        public RoundFunc(AbstractExpression value)
        {
            AssertNotNull(value, nameof(value));
            AssertNumeric(value, nameof(value));
            Arguments.Add(value);
            ReturnType = typeof(double);
        }

        public RoundFunc(AbstractExpression value, AbstractExpression digits)
        {
            AssertNotNull(value, nameof(value));
            AssertNumeric(value, nameof(value));
            AssertNotNull(digits, nameof(digits));
            AssertExpressionOfTypes(digits, nameof(digits), typeof(int));
            Arguments.Add(value);
            Arguments.Add(digits);
            ReturnType = typeof(double);
        }

        private static void AssertNumeric(AbstractExpression expression, string argumentName)
        {
            bool isNumber = expression.ReturnType == typeof(byte)
                || expression.ReturnType == typeof(int)
                || expression.ReturnType == typeof(double);
            if (!isNumber)
            {
                throw new ArgumentException("Illegal argument type", argumentName);
            }
        }
    }
}
