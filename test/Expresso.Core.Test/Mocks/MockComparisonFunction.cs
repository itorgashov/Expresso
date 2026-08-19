using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Tests.Core.Mocks
{
    internal class MockComparisonFunction : ComparisonFunction
    {
        public MockComparisonFunction(AbstractExpression arg1, AbstractExpression arg2) : base(arg1, arg2) { }
    }
}
