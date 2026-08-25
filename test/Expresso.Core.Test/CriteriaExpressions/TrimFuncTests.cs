using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class TrimFuncTests
    {
        [Fact]
        public void TrimFunc_ValidArgument_SetsReturnType()
        {
            var func = new TrimFunc(new MockExpressionOfType(typeof(string)));
            Assert.Single(func.Arguments);
            Assert.Equal(typeof(string), func.ReturnType);
        }

        [Fact]
        public void TrimFunc_NullArgument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TrimFunc(null!));
        }

        [Fact]
        public void TrimFunc_InvalidType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TrimFunc(new MockExpressionOfType(typeof(int))));
        }
    }
}
