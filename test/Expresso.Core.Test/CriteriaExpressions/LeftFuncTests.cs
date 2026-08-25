using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class LeftFuncTests
    {
        [Fact]
        public void LeftFunc_ValidArguments_SetsReturnType()
        {
            var str = new MockExpressionOfType(typeof(string));
            var length = new MockExpressionOfType(typeof(int));

            var func = new LeftFunc(str, length);

            Assert.Equal(2, func.Arguments.Count);
            Assert.Equal(typeof(string), func.ReturnType);
        }

        [Fact]
        public void LeftFunc_NullSource_ThrowsArgumentNullException()
        {
            var length = new MockExpressionOfType(typeof(int));
            Assert.Throws<ArgumentNullException>(() => new LeftFunc(null!, length));
        }

        [Fact]
        public void LeftFunc_InvalidSourceType_ThrowsArgumentException()
        {
            var str = new MockExpressionOfType(typeof(int));
            var length = new MockExpressionOfType(typeof(int));
            Assert.Throws<ArgumentException>(() => new LeftFunc(str, length));
        }

        [Fact]
        public void LeftFunc_InvalidLengthType_ThrowsArgumentException()
        {
            var str = new MockExpressionOfType(typeof(string));
            var length = new MockExpressionOfType(typeof(string));
            Assert.Throws<ArgumentException>(() => new LeftFunc(str, length));
        }
    }
}
