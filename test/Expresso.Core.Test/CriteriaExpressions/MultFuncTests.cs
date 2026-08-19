using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class MultFuncTests
    {
        [Theory]
        [InlineData(typeof(byte))]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        public void Constructor_ValidArguments_SetsReturnType(Type validType)
        {
            var argument1 = new MockExpressionOfType(validType);
            var argument2 = new MockExpressionOfType(validType);

            var multFunc = new MultFunc(argument1, argument2);

            Assert.Equal(multFunc.Arguments?.Count, 2);
            Assert.Equal(argument1, multFunc.Arguments?[0]);
            Assert.Equal(argument2, multFunc.Arguments?[1]);
            Assert.Equal(validType, multFunc.ReturnType);
        }

        [Fact]
        public void Constructor_SetsReturnTypeAsFirstArgType()
        {
            var argument1 = new MockExpressionOfType(typeof(double));
            var argument2 = new MockExpressionOfType(typeof(int));

            var multFunc = new MultFunc(argument1, argument2);

            Assert.Equal(multFunc.Arguments?.Count, 2);
            Assert.Equal(argument1, multFunc.Arguments?[0]);
            Assert.Equal(argument2, multFunc.Arguments?[1]);
            Assert.Equal(argument1.ReturnType, multFunc.ReturnType);
        }


        [Fact]
        public void Constructor_1stNullArgument_ThrowsArgumentNullException()
        {
            var argument2 = new MockExpressionOfType(typeof(int));

            Assert.Throws<ArgumentNullException>(() => new MultFunc(null!, argument2));
        }

        [Fact]
        public void Constructor_2ndNullArgument_ThrowsArgumentNullException()
        {
            var argument1 = new MockExpressionOfType(typeof(int));

            Assert.Throws<ArgumentNullException>(() => new MultFunc(argument1, null!));
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(DateTime))]
        public void Constructor_Invalid1stArgumentType_ThrowsArgumentException(Type invalidType)
        {
            var argument1 = new MockExpressionOfType(invalidType);
            var argument2 = new MockExpressionOfType(typeof(int));

            Assert.Throws<ArgumentException>(() => new MultFunc(argument1, argument2));
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(DateTime))]
        public void Constructor_Invalid2ndArgumentType_ThrowsArgumentException(Type invalidType)
        {
            var argument1 = new MockExpressionOfType(typeof(int));
            var argument2 = new MockExpressionOfType(invalidType);

            Assert.Throws<ArgumentException>(() => new MultFunc(argument1, argument2));
        }
    }
}

