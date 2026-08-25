using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class LeftFunc : StringFunction
    {
        public LeftFunc(AbstractExpression sourceString, AbstractExpression length)
        {
            AssertNotNull(sourceString, nameof(sourceString));
            AssertExpressionOfTypes(sourceString, nameof(sourceString), typeof(string));
            AssertNotNull(length, nameof(length));
            AssertExpressionOfTypes(length, nameof(length), typeof(int));

            Arguments.Add(sourceString);
            Arguments.Add(length);
        }
    }
}
