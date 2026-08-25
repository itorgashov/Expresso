using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class LTrimFuncTests
    {
        [Fact]
        public void LTrimFunc_ValidArgument_SetsReturnType()
        {
            var func = new LTrimFunc(new MockExpressionOfType(typeof(string)));
            Assert.Single(func.Arguments);
            Assert.Equal(typeof(string), func.ReturnType);
        }

        [Fact]
        public void LTrimFunc_NullArgument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LTrimFunc(null!));
        }

        [Fact]
        public void LTrimFunc_InvalidType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new LTrimFunc(new MockExpressionOfType(typeof(int))));
        }
    }
}
