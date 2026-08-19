using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class IsNullFuncTests
    {
        [Theory]
        [InlineData(typeof(bool))]
        [InlineData(typeof(string))]
        [InlineData(typeof(byte))]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        [InlineData(typeof(DateTime))]
        public void IsNullFunc_ValidArgumentTypes_SetsReturnType(Type argumentType)
        {
            var argument = new MockExpressionOfType(argumentType);

            var isNullFunc = new IsNullFunc(argument);

            Assert.Single(isNullFunc.Arguments);
            Assert.Equal(typeof(bool), isNullFunc.ReturnType);
        }

        [Fact]
        public void IsNullFunc_NullArgument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new IsNullFunc(null));
        }

        [Theory]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(float))]
        [InlineData(typeof(long))]
        public void IsNullFunc_InvalidArgumentType_ThrowsArgumentException(Type invalidType)
        {
            var argument = new MockExpressionOfType(invalidType);

            var exception = Assert.Throws<ArgumentException>(() => new IsNullFunc(argument));
        }

        [Fact]
        public void Equals_ReturnsTrue_ForSameArgument()
        {
            Field field = new("Name", typeof(string));
            IsNullFunc func1 = new(field);
            IsNullFunc func2 = new(field);

            Assert.True(func1.Equals(func2));
            Assert.True(func1 == func2);
            Assert.False(func1 != func2);
        }

        [Fact]
        public void Equals_ReturnsFalse_ForDifferentArgument()
        {
            Field field1 = new("Name", typeof(string));
            Field field2 = new("Age", typeof(int));
            IsNullFunc func1 = new(field1);
            IsNullFunc func2 = new(field2);

            Assert.False(func1.Equals(func2));
            Assert.False(func1 == func2);
            Assert.True(func1 != func2);
        }

        [Fact]
        public void GetHashCode_Matches_ForEqualFunctions()
        {
            Field field = new("Name", typeof(string));
            IsNullFunc func1 = new(field);
            IsNullFunc func2 = new(field);

            Assert.Equal(func1.GetHashCode(), func2.GetHashCode());
        }
    }
}


