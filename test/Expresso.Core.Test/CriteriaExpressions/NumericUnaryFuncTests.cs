using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class NumericUnaryFuncTests
    {
        private static readonly AbstractExpression InvalidArg = new MockExpressionOfType(typeof(string));

        [Theory]
        [InlineData(typeof(byte))]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        public void FloorFunc_ValidArgument_ReturnsDouble(Type validType)
        {
            var argument = new MockExpressionOfType(validType);
            var func = new FloorFunc(argument);

            Assert.Single(func.Arguments);
            Assert.Equal(typeof(double), func.ReturnType);
        }

        [Theory]
        [InlineData(typeof(byte))]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        public void CeilingFunc_ValidArgument_ReturnsDouble(Type validType)
        {
            var argument = new MockExpressionOfType(validType);
            var func = new CeilingFunc(argument);

            Assert.Single(func.Arguments);
            Assert.Equal(typeof(double), func.ReturnType);
        }

        [Theory]
        [InlineData(typeof(byte))]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        public void SqrtFunc_ValidArgument_ReturnsDouble(Type validType)
        {
            var argument = new MockExpressionOfType(validType);
            var func = new SqrtFunc(argument);

            Assert.Single(func.Arguments);
            Assert.Equal(typeof(double), func.ReturnType);
        }

        [Theory]
        [InlineData(typeof(byte))]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        public void SignFunc_ValidArgument_ReturnsInt(Type validType)
        {
            var argument = new MockExpressionOfType(validType);
            var func = new SignFunc(argument);

            Assert.Single(func.Arguments);
            Assert.Equal(typeof(int), func.ReturnType);
        }

        [Theory]
        [InlineData(typeof(FloorFunc))]
        [InlineData(typeof(CeilingFunc))]
        [InlineData(typeof(SqrtFunc))]
        [InlineData(typeof(SignFunc))]
        public void NumericUnary_NullArgument_ThrowsArgumentNullException(Type funcType)
        {
            Assert.Throws<ArgumentNullException>(() => CreateUnary(funcType, null!));
        }

        [Theory]
        [InlineData(typeof(FloorFunc))]
        [InlineData(typeof(CeilingFunc))]
        [InlineData(typeof(SqrtFunc))]
        [InlineData(typeof(SignFunc))]
        public void NumericUnary_InvalidArgumentType_ThrowsArgumentException(Type funcType)
        {
            Assert.Throws<ArgumentException>(() => CreateUnary(funcType, InvalidArg));
        }

        private static AbstractFunction CreateUnary(Type funcType, AbstractExpression argument) =>
            funcType.Name switch
            {
                nameof(FloorFunc) => new FloorFunc(argument),
                nameof(CeilingFunc) => new CeilingFunc(argument),
                nameof(SqrtFunc) => new SqrtFunc(argument),
                nameof(SignFunc) => new SignFunc(argument),
                _ => throw new ArgumentOutOfRangeException(nameof(funcType)),
            };
    }
}
