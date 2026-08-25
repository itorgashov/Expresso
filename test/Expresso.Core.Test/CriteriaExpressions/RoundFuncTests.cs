using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class RoundFuncTests
    {
        private static readonly MockExpressionOfType IntArg = new(typeof(int));
        private static readonly MockExpressionOfType InvalidArg = new(typeof(string));

        [Fact]
        public void RoundFunc_OneArgument_ReturnsDouble()
        {
            var func = new RoundFunc(IntArg);

            Assert.Single(func.Arguments);
            Assert.Equal(typeof(double), func.ReturnType);
        }

        [Fact]
        public void RoundFunc_TwoArguments_ReturnsDouble()
        {
            var digits = new Literal(-1);
            var func = new RoundFunc(IntArg, digits);

            Assert.Equal(2, func.Arguments.Count);
            Assert.Equal(typeof(double), func.ReturnType);
        }

        [Fact]
        public void RoundFunc_NullValue_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RoundFunc(null!));
        }

        [Fact]
        public void RoundFunc_NullDigits_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RoundFunc(IntArg, null!));
        }

        [Fact]
        public void RoundFunc_InvalidValueType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new RoundFunc(InvalidArg));
        }

        [Fact]
        public void RoundFunc_InvalidDigitsType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new RoundFunc(IntArg, InvalidArg));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public void RoundFunc_ZeroOrNegativeDigits_IsAllowed(int digits)
        {
            var func = new RoundFunc(IntArg, new Literal(digits));
            Assert.Equal(digits, ((Literal)func.Arguments[1]).Value);
        }
    }
}
