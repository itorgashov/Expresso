using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class OrFuncTests
    {
        private class MockExpression : AbstractExpression
        {
            public MockExpression(Type returnType)
            {
                ReturnType = returnType;
            }
        }

        [Fact]
        public void OrFunc_ValidArguments_SetsReturnType()
        {
            var arg1 = new MockExpression(typeof(bool));
            var arg2 = new MockExpression(typeof(bool));
            var arguments = new List<AbstractExpression> { arg1, arg2 };

            var orFunc = new OrFunc(arguments);

            Assert.Equal(2, orFunc.Arguments.Count);
            Assert.Equal(arg1, orFunc.Arguments[0]);
            Assert.Equal(arg2, orFunc.Arguments[1]);
            Assert.Equal(typeof(bool), orFunc.ReturnType);
        }

        [Fact]
        public void OrFunc_NullArguments_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new OrFunc(null));
        }

        [Fact]
        public void OrFunc_EmptyArguments_ThrowsArgumentException()
        {
            var arguments = new List<AbstractExpression>();

            var exception = Assert.Throws<ArgumentException>(() => new OrFunc(arguments));
        }

        [Fact]
        public void OrFunc_ContainsNullArgument_ThrowsArgumentException()
        {
            var arguments = new List<AbstractExpression> { new MockExpression(typeof(bool)), null! };

            var exception = Assert.Throws<ArgumentException>(() => new OrFunc(arguments));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        [InlineData(typeof(string))]
        [InlineData(typeof(DateTime))]
        public void OrFunc_InvalidArgumentType_ThrowsArgumentException(Type invalidType)
        {
            var arguments = new List<AbstractExpression> { new MockExpression(typeof(bool)), new MockExpression(invalidType) };

            var exception = Assert.Throws<ArgumentException>(() => new OrFunc(arguments));
        }
    }
}
