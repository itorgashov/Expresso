using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class NotFuncTests
    {
        [Fact]
        public void NotFunc_ValidBooleanArgument_SetsReturnType()
        {
            var argument = new MockExpressionOfType(typeof(bool));

            var notFunc = new NotFunc(argument);

            Assert.Single(notFunc.Arguments);
            Assert.Equal(typeof(bool), notFunc.ReturnType);
        }

        [Fact]
        public void NotFunc_NullArgument_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => new NotFunc(null));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        [InlineData(typeof(string))]
        [InlineData(typeof(DateTime))]
        public void NotFunc_InvalidArgumentType_ThrowsArgumentException(Type invalidType)
        {
            var argument = new MockExpressionOfType(invalidType);

            var exception = Assert.Throws<ArgumentException>(() => new NotFunc(argument));
        }
    }

}
