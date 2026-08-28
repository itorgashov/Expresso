using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class GtFuncTests
    {
        [Theory]
        [InlineData(typeof(byte), typeof(byte))]
        [InlineData(typeof(int), typeof(int))]
        [InlineData(typeof(double), typeof(double))]
        [InlineData(typeof(byte), typeof(int))]
        [InlineData(typeof(byte), typeof(double))]
        [InlineData(typeof(int), typeof(byte))]
        [InlineData(typeof(int), typeof(double))]
        [InlineData(typeof(double), typeof(byte))]
        [InlineData(typeof(double), typeof(int))]
        [InlineData(typeof(DateTime), typeof(DateTime))]
        public void NumGtFunc_ValidOperands_SetsArguments(Type leftType, Type rightType)
        {
            var left = new MockExpressionOfType(leftType);
            var right = new MockExpressionOfType(rightType);

            var func = new GtFunc(left, right);

            Assert.Equal(2, func.Arguments.Count);
            Assert.Equal(typeof(bool), func.ReturnType);
        }

        [Theory]
        [InlineData(typeof(string), typeof(string))]
        [InlineData(typeof(string), typeof(bool))]
        [InlineData(typeof(string), typeof(int))]
        [InlineData(typeof(string), typeof(double))]
        [InlineData(typeof(string), typeof(DateTime))]
        [InlineData(typeof(bool), typeof(bool))]
        [InlineData(typeof(bool), typeof(int))]
        [InlineData(typeof(bool), typeof(double))]
        [InlineData(typeof(bool), typeof(string))]
        [InlineData(typeof(bool), typeof(DateTime))]
        [InlineData(typeof(DateTime), typeof(int))]
        [InlineData(typeof(DateTime), typeof(double))]
        [InlineData(typeof(DateTime), typeof(string))]
        [InlineData(typeof(DateTime), typeof(bool))]
        [InlineData(typeof(int), typeof(string))]
        [InlineData(typeof(int), typeof(DateTime))]
        [InlineData(typeof(int), typeof(bool))]
        [InlineData(typeof(Guid), typeof(Guid))]
#if NET6_0_OR_GREATER
        [InlineData(typeof(DateOnly), typeof(DateTime))]
        [InlineData(typeof(DateTime), typeof(DateOnly))]
#endif
        public void NumGtFunc_InvalidOperands_ThrowsArgumentException(Type leftType, Type rightType)
        {
            var left = new MockExpressionOfType(leftType);
            var right = new MockExpressionOfType(rightType);

            Assert.Throws<ArgumentException>(() => new GtFunc(left, right));
        }

        [Fact]
        public void NumGtFunc_NullLeftOperand_ThrowsArgumentNullException()
        {
            var right = new MockExpressionOfType(typeof(int));

            Assert.Throws<ArgumentNullException>(() => new GtFunc(null, right));
        }

        [Fact]
        public void NumGtFunc_NullRightOperand_ThrowsArgumentNullException()
        {
            var left = new MockExpressionOfType(typeof(int));

            Assert.Throws<ArgumentNullException>(() => new GtFunc(left, null));
        }

        [Fact]
        public void Equals_ReturnsTrue_ForSameOperands()
        {
            Field left = new("Age", typeof(int));
            Literal right = new(30);
            GtFunc fn1 = new(left, right);
            GtFunc fn2 = new(left, right);

            Assert.True(fn1.Equals(fn2));
            Assert.True(fn1 == fn2);
            Assert.False(fn1 != fn2);
        }

        [Fact]
        public void GetHashCode_Matches_ForEqualFunctions()
        {
            Field left = new("Age", typeof(int));
            Literal right = new(30);
            GtFunc fn1 = new(left, right);
            GtFunc fn2 = new(left, right);

            Assert.Equal(fn1.GetHashCode(), fn2.GetHashCode());
        }


        [Fact]
        public void Equals_ReturnsFalse_ForDifferentFunctionTypes()
        {
            Field left = new("Age", typeof(int));
            Literal right = new(30);
            GtFunc fn1 = new(left, right);
            MockComparisonFunction fn2 = new(left, right);

            Assert.False(fn1.Equals(fn2));
            Assert.False(fn1 == fn2);
            Assert.True(fn1 != fn2);
        }
    }
}
