using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class TimeFuncTests
    {
        [Fact]
        public void TimeFunc_ValidDateTimeArgument_ReturnsTimeOfDayType()
        {
            var func = new TimeFunc(new MockExpressionOfType(typeof(DateTime)));
            Assert.Single(func.Arguments);
#if NET6_0_OR_GREATER
            Assert.Equal(typeof(TimeOnly), func.ReturnType);
#else
            Assert.Equal(typeof(TimeSpan), func.ReturnType);
#endif
        }

        [Fact]
        public void TimeFunc_ValidStringArgument_ReturnsTimeOfDayType()
        {
            var func = new TimeFunc(new MockExpressionOfType(typeof(string)));
#if NET6_0_OR_GREATER
            Assert.Equal(typeof(TimeOnly), func.ReturnType);
#else
            Assert.Equal(typeof(TimeSpan), func.ReturnType);
#endif
        }

#if NET6_0_OR_GREATER
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
#else
        [Fact]
        public void TimeFunc_ValidTimeSpanArgument_ReturnsTimeSpan()
        {
            var func = new TimeFunc(new MockExpressionOfType(typeof(TimeSpan)));
            Assert.Equal(typeof(TimeSpan), func.ReturnType);
        }
#endif

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
