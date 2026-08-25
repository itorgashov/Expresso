using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class NumericBinaryFuncTests
    {
        private static readonly AbstractExpression InvalidArg = new MockExpressionOfType(typeof(string));

        [Theory]
        [InlineData(typeof(byte))]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        public void ModFunc_ValidArguments_ReturnsFirstArgType(Type validType)
        {
            var argument1 = new MockExpressionOfType(validType);
            var argument2 = new MockExpressionOfType(typeof(int));

            var func = new ModFunc(argument1, argument2);

            Assert.Equal(2, func.Arguments.Count);
            Assert.Equal(validType, func.ReturnType);
        }

        [Fact]
        public void PowerFunc_ValidArguments_ReturnsDouble()
        {
            var argument1 = new MockExpressionOfType(typeof(int));
            var argument2 = new MockExpressionOfType(typeof(int));

            var func = new PowerFunc(argument1, argument2);

            Assert.Equal(typeof(double), func.ReturnType);
        }

        [Theory]
        [InlineData(typeof(byte))]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        public void MinFunc_ValidArguments_ReturnsFirstArgType(Type validType)
        {
            var argument1 = new MockExpressionOfType(validType);
            var argument2 = new MockExpressionOfType(typeof(int));

            var func = new MinFunc(argument1, argument2);

            Assert.Equal(validType, func.ReturnType);
        }

        [Theory]
        [InlineData(typeof(byte))]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        public void MaxFunc_ValidArguments_ReturnsFirstArgType(Type validType)
        {
            var argument1 = new MockExpressionOfType(validType);
            var argument2 = new MockExpressionOfType(typeof(int));

            var func = new MaxFunc(argument1, argument2);

            Assert.Equal(validType, func.ReturnType);
        }

        [Theory]
        [InlineData(typeof(ModFunc))]
        [InlineData(typeof(PowerFunc))]
        [InlineData(typeof(MinFunc))]
        [InlineData(typeof(MaxFunc))]
        public void NumericBinary_NullFirstArgument_ThrowsArgumentNullException(Type funcType)
        {
            var argument2 = new MockExpressionOfType(typeof(int));
            Assert.Throws<ArgumentNullException>(() => CreateBinary(funcType, null!, argument2));
        }

        [Theory]
        [InlineData(typeof(ModFunc))]
        [InlineData(typeof(PowerFunc))]
        [InlineData(typeof(MinFunc))]
        [InlineData(typeof(MaxFunc))]
        public void NumericBinary_NullSecondArgument_ThrowsArgumentNullException(Type funcType)
        {
            var argument1 = new MockExpressionOfType(typeof(int));
            Assert.Throws<ArgumentNullException>(() => CreateBinary(funcType, argument1, null!));
        }

        [Theory]
        [InlineData(typeof(ModFunc))]
        [InlineData(typeof(PowerFunc))]
        [InlineData(typeof(MinFunc))]
        [InlineData(typeof(MaxFunc))]
        public void NumericBinary_InvalidFirstArgumentType_ThrowsArgumentException(Type funcType)
        {
            var argument2 = new MockExpressionOfType(typeof(int));
            Assert.Throws<ArgumentException>(() => CreateBinary(funcType, InvalidArg, argument2));
        }

        [Theory]
        [InlineData(typeof(ModFunc))]
        [InlineData(typeof(PowerFunc))]
        [InlineData(typeof(MinFunc))]
        [InlineData(typeof(MaxFunc))]
        public void NumericBinary_InvalidSecondArgumentType_ThrowsArgumentException(Type funcType)
        {
            var argument1 = new MockExpressionOfType(typeof(int));
            Assert.Throws<ArgumentException>(() => CreateBinary(funcType, argument1, InvalidArg));
        }

        private static AbstractFunction CreateBinary(Type funcType, AbstractExpression argument1, AbstractExpression argument2) =>
            funcType.Name switch
            {
                nameof(ModFunc) => new ModFunc(argument1, argument2),
                nameof(PowerFunc) => new PowerFunc(argument1, argument2),
                nameof(MinFunc) => new MinFunc(argument1, argument2),
                nameof(MaxFunc) => new MaxFunc(argument1, argument2),
                _ => throw new ArgumentOutOfRangeException(nameof(funcType)),
            };
    }
}
