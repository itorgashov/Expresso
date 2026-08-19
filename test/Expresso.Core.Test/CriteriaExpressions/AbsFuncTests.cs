using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class AbsFuncTests
    {
        [Theory]
        [InlineData(typeof(byte))]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        public void Constructor_ValidArgument_SetsReturnType(Type validType)
        {
            var argument = new MockExpressionOfType(validType);

            var absFunc = new AbsFunc(argument);

            Assert.Single(absFunc.Arguments);
            Assert.Equal(argument, absFunc.Arguments[0]);
            Assert.Equal(validType, absFunc.ReturnType);
        }

        [Fact]
        public void Constructor_NullArgument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AbsFunc(null));
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(DateTime))]
        public void Constructor_InvalidArgumentType_ThrowsArgumentException(Type invalidType)
        {

            var argument = new MockExpressionOfType(invalidType);

            Assert.Throws<ArgumentException>(() => new AbsFunc(argument));
        }
    }
}
