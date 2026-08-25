using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class StrContainsFunc : BooleanFunction
    {
        public StrContainsFunc(AbstractExpression testExpression, AbstractExpression matchToExpression)
        {
            AssertNotNull(testExpression, nameof(testExpression));
            AssertExpressionOfTypes(testExpression, nameof(testExpression), typeof(string));
            AssertNotNull(matchToExpression, nameof(matchToExpression));
            AssertExpressionOfTypes(matchToExpression, nameof(matchToExpression), typeof(string));

            Arguments.Add(testExpression);
            Arguments.Add(matchToExpression);
        }
    }
}
