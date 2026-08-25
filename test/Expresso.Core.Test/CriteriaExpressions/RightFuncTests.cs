using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class RightFuncTests
    {
        [Fact]
        public void RightFunc_ValidArguments_SetsReturnType()
        {
            var str = new MockExpressionOfType(typeof(string));
            var length = new MockExpressionOfType(typeof(int));

            var func = new RightFunc(str, length);

            Assert.Equal(2, func.Arguments.Count);
            Assert.Equal(typeof(string), func.ReturnType);
        }

        [Fact]
        public void RightFunc_NullSource_ThrowsArgumentNullException()
        {
            var length = new MockExpressionOfType(typeof(int));
            Assert.Throws<ArgumentNullException>(() => new RightFunc(null!, length));
        }

        [Fact]
        public void RightFunc_InvalidSourceType_ThrowsArgumentException()
        {
            var str = new MockExpressionOfType(typeof(int));
            var length = new MockExpressionOfType(typeof(int));
            Assert.Throws<ArgumentException>(() => new RightFunc(str, length));
        }

        [Fact]
        public void RightFunc_InvalidLengthType_ThrowsArgumentException()
        {
            var str = new MockExpressionOfType(typeof(string));
            var length = new MockExpressionOfType(typeof(string));
            Assert.Throws<ArgumentException>(() => new RightFunc(str, length));
        }
    }
}
