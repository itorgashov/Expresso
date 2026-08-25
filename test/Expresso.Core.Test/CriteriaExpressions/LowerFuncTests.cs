using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class LowerFuncTests
    {
        [Fact]
        public void LowerFunc_ValidArgument_SetsReturnType()
        {
            var str = new MockExpressionOfType(typeof(string));
            var func = new LowerFunc(str);
            Assert.Single(func.Arguments);
            Assert.Equal(typeof(string), func.ReturnType);
        }

        [Fact]
        public void LowerFunc_NullArgument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LowerFunc(null!));
        }

        [Fact]
        public void LowerFunc_InvalidType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new LowerFunc(new MockExpressionOfType(typeof(int))));
        }
    }
}
