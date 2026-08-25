using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class RTrimFuncTests
    {
        [Fact]
        public void RTrimFunc_ValidArgument_SetsReturnType()
        {
            var func = new RTrimFunc(new MockExpressionOfType(typeof(string)));
            Assert.Single(func.Arguments);
            Assert.Equal(typeof(string), func.ReturnType);
        }

        [Fact]
        public void RTrimFunc_NullArgument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RTrimFunc(null!));
        }

        [Fact]
        public void RTrimFunc_InvalidType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new RTrimFunc(new MockExpressionOfType(typeof(int))));
        }
    }
}
