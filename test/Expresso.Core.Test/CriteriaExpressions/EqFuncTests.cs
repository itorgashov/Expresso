using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class EqFuncTests
    {
        [Theory]
        [InlineData(typeof(byte), typeof(byte))]
        [InlineData(typeof(int), typeof(int))]
        [InlineData(typeof(double), typeof(double))]
        [InlineData(typeof(bool), typeof(bool))]
        [InlineData(typeof(string), typeof(string))]
        [InlineData(typeof(DateTime), typeof(DateTime))]
        [InlineData(typeof(byte), typeof(int))]
        [InlineData(typeof(byte), typeof(double))]
        [InlineData(typeof(int), typeof(byte))]
        [InlineData(typeof(int), typeof(double))]
        [InlineData(typeof(double), typeof(byte))]
        [InlineData(typeof(double), typeof(int))]
        public void NumEqFunc_ValidOperands_SetsArguments(Type leftType, Type rightType)
        {
            var left = new MockExpressionOfType(leftType);
            var right = new MockExpressionOfType(rightType);

            var eqFunc = new EqFunc(left, right);

            Assert.Equal(2, eqFunc.Arguments.Count);
            Assert.Equal(typeof(bool), eqFunc.ReturnType);
        }

        [Theory]
        [InlineData(typeof(int), typeof(string))]
        [InlineData(typeof(string), typeof(double))]
        [InlineData(typeof(bool), typeof(DateTime))]
        public void NumEqFunc_InvalidOperands_ThrowsArgumentException(Type leftType, Type rightType)
        {
            var left = new MockExpressionOfType(leftType);
            var right = new MockExpressionOfType(rightType);

            Assert.Throws<ArgumentException>(() => new EqFunc(left, right));
        }

        [Fact]
        public void NumEqFunc_NullLeftOperand_ThrowsArgumentNullException()
        {
            var right = new MockExpressionOfType(typeof(int));

            Assert.Throws<ArgumentNullException>(() => new EqFunc(null!, right));
        }

        [Fact]
        public void NumEqFunc_NullRightOperand_ThrowsArgumentNullException()
        {
            var left = new MockExpressionOfType(typeof(int));

            Assert.Throws<ArgumentNullException>(() => new EqFunc(left, null!));
        }

        [Fact]
        public void Equals_ReturnsTrue_ForSameOperands()
        {
            Field left = new("Age", typeof(int));
            Literal right = new(30);
            EqFunc fn1 = new(left, right);
            EqFunc fn2 = new(left, right);

            Assert.True(fn1.Equals(fn2));
            Assert.True(fn1 == fn2);
            Assert.False(fn1 != fn2);
        }

        [Fact]
        public void GetHashCode_Matches_ForEqualFunctions()
        {
            Field left = new("Age", typeof(int));
            Literal right = new(30);
            EqFunc fn1 = new(left, right);
            EqFunc fn2 = new(left, right);

            Assert.Equal(fn1.GetHashCode(), fn2.GetHashCode());
        }

        [Fact]
        public void Equals_ReturnsFalse_ForDifferentFunctionTypes()
        {
            Field left = new("Age", typeof(int));
            Literal right = new(30);
            EqFunc fn1 = new(left, right);
            MockComparisonFunction fn2 = new(left, right);

            Assert.False(fn1.Equals(fn2));
            Assert.False(fn1 == fn2);
            Assert.True(fn1 != fn2);
        }
    }
}
