using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class ReplaceFuncTests
    {
        [Fact]
        public void ReplaceFunc_ValidArguments_SetsReturnType()
        {
            var source = new MockExpressionOfType(typeof(string));
            var oldValue = new MockExpressionOfType(typeof(string));
            var newValue = new MockExpressionOfType(typeof(string));

            var func = new ReplaceFunc(source, oldValue, newValue);

            Assert.Equal(3, func.Arguments.Count);
            Assert.Equal(typeof(string), func.ReturnType);
        }

        [Fact]
        public void ReplaceFunc_NullSource_ThrowsArgumentNullException()
        {
            var oldValue = new MockExpressionOfType(typeof(string));
            var newValue = new MockExpressionOfType(typeof(string));
            Assert.Throws<ArgumentNullException>(() => new ReplaceFunc(null!, oldValue, newValue));
        }

        [Fact]
        public void ReplaceFunc_InvalidOldValueType_ThrowsArgumentException()
        {
            var source = new MockExpressionOfType(typeof(string));
            var oldValue = new MockExpressionOfType(typeof(int));
            var newValue = new MockExpressionOfType(typeof(string));
            Assert.Throws<ArgumentException>(() => new ReplaceFunc(source, oldValue, newValue));
        }
    }
}
