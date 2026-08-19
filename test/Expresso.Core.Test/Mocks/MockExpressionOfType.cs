using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Tests.Core.Mocks
{
    internal class MockExpressionOfType : AbstractExpression
    {
        public MockExpressionOfType(Type returnType)
        {
            ReturnType = returnType;
        }
    }
}
