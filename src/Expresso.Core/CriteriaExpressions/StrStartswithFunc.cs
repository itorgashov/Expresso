using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class StrStartswithFunc : BooleanFunction
    {
        public StrStartswithFunc(AbstractExpression testExpression, AbstractExpression matchToExpression)
        {
            AssertNotNull(testExpression, nameof(testExpression));
            AssertExpressionOfTypes(testExpression, nameof(testExpression), typeof(string));
            AssertNotNull(matchToExpression, nameof(matchToExpression));
            AssertExpressionOfTypes(matchToExpression, nameof(matchToExpression), typeof(string));

            Arguments.Add(testExpression);
            Arguments.Add(matchToExpression);
        }

        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
