using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class StrInFuncTests
    {
        [Fact]
        public void StrInFunc_ValidArguments_SetsArguments()
        {
            var args = new List<AbstractExpression>
            {
                new MockExpressionOfType(typeof(string)),
                new MockExpressionOfType(typeof(string))
            };

            var func = new InFunc(args);

            Assert.Equal(2, func.Arguments.Count);
        }

        [Theory]
        [InlineData(typeof(byte))]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        [InlineData(typeof(string))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(bool))]
        public void InFunc_ValidSameTypeArguments_SetsArguments(Type validType)
        {
            var args = new List<AbstractExpression>
            {
                new MockExpressionOfType(validType),
                new MockExpressionOfType(validType),
                new MockExpressionOfType(validType)
            };

            var func = new InFunc(args);

            Assert.Equal(3, func.Arguments.Count);
            Assert.Equal(typeof(bool), func.ReturnType);
        }

        [Fact]
        public void StrInFunc_NullArguments_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new InFunc(null!));
        }

        [Fact]
        public void StrInFunc_EmptyArguments_ThrowsArgumentException()
        {
            var args = new List<AbstractExpression>();

            Assert.Throws<ArgumentException>(() => new InFunc(args));
        }

        [Fact]
        public void StrInFunc_NullElementInArguments_ThrowsArgumentException()
        {
            var args = new List<AbstractExpression>
            {
                new MockExpressionOfType(typeof(string)),
                null!
            };

            Assert.Throws<ArgumentException>(() => new InFunc(args));
        }

        [Theory]
        [InlineData(typeof(byte))]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(bool))]
        public void StrInFunc_InvalidArgumentType_ThrowsArgumentException(Type invalidType)
        {
            var args = new List<AbstractExpression>
            {
                new MockExpressionOfType(typeof(string)),
                new MockExpressionOfType(invalidType)
            };

            Assert.Throws<ArgumentException>(() => new InFunc(args));
        }
    }

}
