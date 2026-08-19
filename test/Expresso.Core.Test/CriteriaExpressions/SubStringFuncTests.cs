using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class SubStringFuncTests
    {
        [Fact]
        public void SubStringFunc_ValidArguments_SetsReturnType()
        {
            var str = new MockExpressionOfType(typeof(string));
            var startIndex = new MockExpressionOfType(typeof(int));
            var length = new MockExpressionOfType(typeof(int));

            var subStringFunc = new SubStringFunc(str, startIndex, length);

            Assert.Equal(3, subStringFunc.Arguments.Count);
            Assert.Equal(typeof(string), subStringFunc.ReturnType);
        }

        [Theory]
        [InlineData(null, typeof(int), typeof(int), "str")]
        [InlineData(typeof(string), null, typeof(int), "startIndex")]
        [InlineData(typeof(string), typeof(int), null, "length")]
        public void SubStringFunc_NullArgument_ThrowsArgumentNullException(Type strType, Type startIndexType, Type lengthType, string paramName)
        {
            var str = strType == null ? null : new MockExpressionOfType(strType);
            var startIndex = startIndexType == null ? null : new MockExpressionOfType(startIndexType);
            var length = lengthType == null ? null : new MockExpressionOfType(lengthType);

            var exception = Assert.Throws<ArgumentNullException>(() => new SubStringFunc(str, startIndex, length));
        }

        [Theory]
        [InlineData(typeof(int), typeof(int), typeof(int), "str")]
        [InlineData(typeof(double), typeof(int), typeof(int), "str")]
        [InlineData(typeof(DateTime), typeof(int), typeof(int), "str")]
        [InlineData(typeof(string), typeof(string), typeof(int), "startIndex")]
        [InlineData(typeof(string), typeof(double), typeof(int), "startIndex")]
        [InlineData(typeof(string), typeof(DateTime), typeof(int), "startIndex")]
        [InlineData(typeof(string), typeof(int), typeof(string), "length")]
        [InlineData(typeof(string), typeof(int), typeof(double), "length")]
        [InlineData(typeof(string), typeof(int), typeof(DateTime), "length")]
        public void SubStringFunc_InvalidArgumentType_ThrowsArgumentException(Type strType, Type startIndexType, Type lengthType, string paramName)
        {
            var str = new MockExpressionOfType(strType);
            var startIndex = new MockExpressionOfType(startIndexType);
            var length = new MockExpressionOfType(lengthType);

            var exception = Assert.Throws<ArgumentException>(() => new SubStringFunc(str, startIndex, length));
        }
    }
}
