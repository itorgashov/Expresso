using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class AndFuncTests
    {
        [Fact]
        public void AndFunc_ValidArguments_SetsReturnType()
        {

            var arg1 = new MockExpressionOfType(typeof(bool));
            var arg2 = new MockExpressionOfType(typeof(bool));
            var arguments = new List<AbstractExpression> { arg1, arg2 };

            var andFunc = new AndFunc(arguments);

            Assert.Equal(2, andFunc.Arguments.Count);
            Assert.Equal(arg1, andFunc.Arguments[0]);
            Assert.Equal(arg2, andFunc.Arguments[1]);
            Assert.Equal(typeof(bool), andFunc.ReturnType);
        }

        [Fact]
        public void AndFunc_NullArguments_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AndFunc(null));
        }

        [Fact]
        public void AndFunc_EmptyArguments_ThrowsArgumentException()
        {
            var arguments = new List<AbstractExpression>();

            var exception = Assert.Throws<ArgumentException>(() => new AndFunc(arguments));
        }

        [Fact]
        public void AndFunc_ContainsNullArgument_ThrowsArgumentException()
        {
            var arguments = new List<AbstractExpression> { new MockExpressionOfType(typeof(bool)), null! };

            var exception = Assert.Throws<ArgumentException>(() => new AndFunc(arguments));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        [InlineData(typeof(string))]
        [InlineData(typeof(DateTime))]
        public void AndFunc_InvalidArgumentType_ThrowsArgumentException(Type invalidType)
        {
            var arguments = new List<AbstractExpression> { new MockExpressionOfType(typeof(bool)), new MockExpressionOfType(invalidType) };

            var exception = Assert.Throws<ArgumentException>(() => new AndFunc(arguments));
        }
    }
}
