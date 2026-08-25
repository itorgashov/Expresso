using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class UpperFuncTests
    {
        [Fact]
        public void UpperFunc_ValidArgument_SetsReturnType()
        {
            var str = new MockExpressionOfType(typeof(string));
            var func = new UpperFunc(str);
            Assert.Single(func.Arguments);
            Assert.Equal(typeof(string), func.ReturnType);
        }

        [Fact]
        public void UpperFunc_NullArgument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new UpperFunc(null!));
        }

        [Fact]
        public void UpperFunc_InvalidType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new UpperFunc(new MockExpressionOfType(typeof(int))));
        }
    }
}
