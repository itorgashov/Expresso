using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class TimeSpanTimeOfDayTests
    {
        private static readonly AbstractExpression TimeSpanArg = new MockExpressionOfType(typeof(TimeSpan));
        private static readonly AbstractExpression IntArg = new MockExpressionOfType(typeof(int));

        [Fact]
        public void EqFunc_TimeSpanOperands_SetsArguments()
        {
            var eqFunc = new EqFunc(TimeSpanArg, new MockExpressionOfType(typeof(TimeSpan)));
            Assert.Equal(2, eqFunc.Arguments.Count);
            Assert.Equal(typeof(bool), eqFunc.ReturnType);
        }

        [Fact]
        public void GtFunc_TimeSpanOperands_SetsArguments()
        {
            var gtFunc = new GtFunc(TimeSpanArg, new MockExpressionOfType(typeof(TimeSpan)));
            Assert.Equal(typeof(bool), gtFunc.ReturnType);
        }

        [Fact]
        public void IsNullFunc_TimeSpanArgument_SetsArguments()
        {
            var isNullFunc = new IsNullFunc(TimeSpanArg);
            Assert.Single(isNullFunc.Arguments);
            Assert.Equal(typeof(bool), isNullFunc.ReturnType);
        }

        [Fact]
        public void HourFunc_TimeSpanArgument_ReturnsInt()
        {
            var func = new HourFunc(TimeSpanArg);
            Assert.Equal(typeof(int), func.ReturnType);
        }

        [Fact]
        public void AddHoursFunc_TimeSpanArgument_ReturnsTimeSpan()
        {
            var func = new AddHoursFunc(TimeSpanArg, IntArg);
            Assert.Equal(typeof(TimeSpan), func.ReturnType);
        }

#if NET6_0_OR_GREATER
        [Fact]
        public void EqFunc_TimeSpanAndTimeOnly_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new EqFunc(TimeSpanArg, new MockExpressionOfType(typeof(TimeOnly))));
        }
#endif
    }
}
