using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class DateTimeGetterFuncTests
    {
        private static readonly AbstractExpression DateTimeArg = new MockExpressionOfType(typeof(DateTime));
        private static readonly AbstractExpression InvalidArg = new MockExpressionOfType(typeof(string));

        [Theory]
        [InlineData(typeof(YearFunc))]
        [InlineData(typeof(MonthFunc))]
        [InlineData(typeof(DayFunc))]
        [InlineData(typeof(DayOfYearFunc))]
        [InlineData(typeof(HourFunc))]
        [InlineData(typeof(MinuteFunc))]
        [InlineData(typeof(SecondFunc))]
        [InlineData(typeof(DayOfWeekFunc))]
        public void DateTimeGetter_ValidArgument_ReturnsInt(Type funcType)
        {
            var func = CreateGetter(funcType, DateTimeArg);
            Assert.Single(func.Arguments);
            Assert.Equal(typeof(int), func.ReturnType);
        }

        [Theory]
        [InlineData(typeof(YearFunc))]
        [InlineData(typeof(MonthFunc))]
        [InlineData(typeof(DayFunc))]
        [InlineData(typeof(DayOfYearFunc))]
        [InlineData(typeof(HourFunc))]
        [InlineData(typeof(MinuteFunc))]
        [InlineData(typeof(SecondFunc))]
        [InlineData(typeof(DayOfWeekFunc))]
        public void DateTimeGetter_NullArgument_ThrowsArgumentNullException(Type funcType)
        {
            Assert.Throws<ArgumentNullException>(() => CreateGetter(funcType, null!));
        }

        [Theory]
        [InlineData(typeof(YearFunc))]
        [InlineData(typeof(MonthFunc))]
        [InlineData(typeof(DayFunc))]
        [InlineData(typeof(DayOfYearFunc))]
        [InlineData(typeof(HourFunc))]
        [InlineData(typeof(MinuteFunc))]
        [InlineData(typeof(SecondFunc))]
        [InlineData(typeof(DayOfWeekFunc))]
        public void DateTimeGetter_InvalidArgumentType_ThrowsArgumentException(Type funcType)
        {
            Assert.Throws<ArgumentException>(() => CreateGetter(funcType, InvalidArg));
        }

        private static AbstractFunction CreateGetter(Type funcType, AbstractExpression argument) =>
            funcType.Name switch
            {
                nameof(YearFunc) => new YearFunc(argument),
                nameof(MonthFunc) => new MonthFunc(argument),
                nameof(DayFunc) => new DayFunc(argument),
                nameof(DayOfYearFunc) => new DayOfYearFunc(argument),
                nameof(HourFunc) => new HourFunc(argument),
                nameof(MinuteFunc) => new MinuteFunc(argument),
                nameof(SecondFunc) => new SecondFunc(argument),
                nameof(DayOfWeekFunc) => new DayOfWeekFunc(argument),
                _ => throw new ArgumentOutOfRangeException(nameof(funcType)),
            };
    }
}
