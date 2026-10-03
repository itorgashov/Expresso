using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Tests.Core.Mocks
{
    internal class MockComparisonFunction : ComparisonFunction
    {
        public MockComparisonFunction(AbstractExpression arg1, AbstractExpression arg2) : base(arg1, arg2) { }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            throw new NotSupportedException();
    }
}
