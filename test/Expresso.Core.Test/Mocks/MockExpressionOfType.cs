using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Tests.Core.Mocks
{
    internal class MockExpressionOfType : AbstractExpression
    {
        public MockExpressionOfType(Type returnType)
        {
            ReturnType = returnType;
        }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            throw new NotSupportedException();
    }
}
