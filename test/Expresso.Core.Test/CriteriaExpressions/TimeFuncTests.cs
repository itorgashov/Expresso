#if NET6_0_OR_GREATER
using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class TimeFuncTests
    {
        [Fact]
        public void TimeFunc_ValidDateTimeArgument_ReturnsTimeOnly()
        {
            var func = new TimeFunc(new MockExpressionOfType(typeof(DateTime)));
            Assert.Single(func.Arguments);
            Assert.Equal(typeof(TimeOnly), func.ReturnType);
        }

        [Fact]
        public void TimeFunc_ValidStringArgument_ReturnsTimeOnly()
        {
            var func = new TimeFunc(new MockExpressionOfType(typeof(string)));
            Assert.Equal(typeof(TimeOnly), func.ReturnType);
        }

        [Fact]
        public void TimeFunc_ValidTimeOnlyArgument_ReturnsTimeOnly()
        {
            var func = new TimeFunc(new MockExpressionOfType(typeof(TimeOnly)));
            Assert.Equal(typeof(TimeOnly), func.ReturnType);
        }

        [Fact]
        public void TimeFunc_DateOnlyArgument_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TimeFunc(new MockExpressionOfType(typeof(DateOnly))));
        }

        [Fact]
        public void TimeFunc_NullArgument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TimeFunc(null!));
        }

        [Fact]
        public void TimeFunc_InvalidArgumentType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TimeFunc(new MockExpressionOfType(typeof(int))));
        }
    }
}
#endif
