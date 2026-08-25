using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class ReplaceFunc : StringFunction
    {
        public ReplaceFunc(AbstractExpression sourceString, AbstractExpression oldValue, AbstractExpression newValue)
        {
            AssertNotNull(sourceString, nameof(sourceString));
            AssertExpressionOfTypes(sourceString, nameof(sourceString), typeof(string));
            AssertNotNull(oldValue, nameof(oldValue));
            AssertExpressionOfTypes(oldValue, nameof(oldValue), typeof(string));
            AssertNotNull(newValue, nameof(newValue));
            AssertExpressionOfTypes(newValue, nameof(newValue), typeof(string));

            Arguments.Add(sourceString);
            Arguments.Add(oldValue);
            Arguments.Add(newValue);
        }
    }
}
