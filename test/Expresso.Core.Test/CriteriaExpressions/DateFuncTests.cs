using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class DateFuncTests
    {
        [Fact]
        public void DateFunc_ValidDateTimeArgument_Succeeds()
        {
            var func = new DateFunc(new MockExpressionOfType(typeof(DateTime)));
            Assert.Single(func.Arguments);
#if NET6_0_OR_GREATER
            Assert.Equal(typeof(DateOnly), func.ReturnType);
#else
            Assert.Equal(typeof(DateTime), func.ReturnType);
#endif
        }

#if NET6_0_OR_GREATER
        [Fact]
        public void DateFunc_ValidStringArgument_ReturnsDateOnly()
        {
            var func = new DateFunc(new MockExpressionOfType(typeof(string)));
            Assert.Equal(typeof(DateOnly), func.ReturnType);
        }

        [Fact]
        public void DateFunc_ValidDateOnlyArgument_ReturnsDateOnly()
        {
            var func = new DateFunc(new MockExpressionOfType(typeof(DateOnly)));
            Assert.Equal(typeof(DateOnly), func.ReturnType);
        }

        [Fact]
        public void DateFunc_TimeOnlyArgument_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new DateFunc(new MockExpressionOfType(typeof(TimeOnly))));
        }
#endif

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
