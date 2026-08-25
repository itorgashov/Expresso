using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class DateFuncTests
    {
        [Fact]
        public void DateFunc_ValidArgument_ReturnsDateTime()
        {
            var func = new DateFunc(new MockExpressionOfType(typeof(DateTime)));
            Assert.Single(func.Arguments);
            Assert.Equal(typeof(DateTime), func.ReturnType);
        }

        [Fact]
        public void DateFunc_NullArgument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new DateFunc(null!));
        }

        [Fact]
        public void DateFunc_InvalidArgumentType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new DateFunc(new MockExpressionOfType(typeof(int))));
        }
    }
}
