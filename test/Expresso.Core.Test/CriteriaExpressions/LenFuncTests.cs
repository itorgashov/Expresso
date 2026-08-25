using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class LenFuncTests
    {
        [Fact]
        public void LenFunc_ValidArgument_SetsReturnTypeToInt()
        {
            var func = new LenFunc(new MockExpressionOfType(typeof(string)));
            Assert.Single(func.Arguments);
            Assert.Equal(typeof(int), func.ReturnType);
        }

        [Fact]
        public void LenFunc_NullArgument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LenFunc(null!));
        }

        [Fact]
        public void LenFunc_InvalidType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new LenFunc(new MockExpressionOfType(typeof(int))));
        }
    }
}
