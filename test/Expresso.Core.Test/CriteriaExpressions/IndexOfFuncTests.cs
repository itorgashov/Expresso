using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class IndexOfFuncTests
    {
        [Fact]
        public void IndexOfFunc_ValidArguments_SetsReturnTypeToInt()
        {
            var source = new MockExpressionOfType(typeof(string));
            var find = new MockExpressionOfType(typeof(string));

            var func = new IndexOfFunc(source, find);

            Assert.Equal(2, func.Arguments.Count);
            Assert.Equal(typeof(int), func.ReturnType);
        }

        [Fact]
        public void IndexOfFunc_NullSource_ThrowsArgumentNullException()
        {
            var find = new MockExpressionOfType(typeof(string));
            Assert.Throws<ArgumentNullException>(() => new IndexOfFunc(null!, find));
        }

        [Fact]
        public void IndexOfFunc_InvalidFindType_ThrowsArgumentException()
        {
            var source = new MockExpressionOfType(typeof(string));
            var find = new MockExpressionOfType(typeof(int));
            Assert.Throws<ArgumentException>(() => new IndexOfFunc(source, find));
        }
    }
}
