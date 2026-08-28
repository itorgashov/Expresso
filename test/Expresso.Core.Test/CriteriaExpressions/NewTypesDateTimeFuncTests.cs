#if NET6_0_OR_GREATER
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class NewTypesDateTimeFuncTests
    {
        private static readonly AbstractExpression DateOnlyArg = new MockExpressionOfType(typeof(DateOnly));
        private static readonly AbstractExpression TimeOnlyArg = new MockExpressionOfType(typeof(TimeOnly));
        private static readonly AbstractExpression IntArg = new MockExpressionOfType(typeof(int));

        [Theory]
        [InlineData(typeof(YearFunc))]
        [InlineData(typeof(MonthFunc))]
        [InlineData(typeof(DayFunc))]
        public void CalendarGetter_AcceptsDateOnly_ReturnsInt(Type funcType)
        {
            var func = CreateCalendarGetter(funcType, DateOnlyArg);
            Assert.Equal(typeof(int), func.ReturnType);
        }

        [Theory]
        [InlineData(typeof(HourFunc))]
        [InlineData(typeof(MinuteFunc))]
        [InlineData(typeof(SecondFunc))]
        public void TimeGetter_AcceptsTimeOnly_ReturnsInt(Type funcType)
        {
            var func = CreateTimeGetter(funcType, TimeOnlyArg);
            Assert.Equal(typeof(int), func.ReturnType);
        }

        [Theory]
        [InlineData(typeof(AddDaysFunc))]
        [InlineData(typeof(AddMonthsFunc))]
        [InlineData(typeof(AddYearsFunc))]
        public void CalendarAdd_AcceptsDateOnly_ReturnsDateOnly(Type funcType)
        {
            var func = CreateAdd(funcType, DateOnlyArg, IntArg);
            Assert.Equal(typeof(DateOnly), func.ReturnType);
        }

        [Theory]
        [InlineData(typeof(AddHoursFunc))]
        [InlineData(typeof(AddMinutesFunc))]
        [InlineData(typeof(AddSecondsFunc))]
        public void TimeAdd_AcceptsTimeOnly_ReturnsTimeOnly(Type funcType)
        {
            var func = CreateAdd(funcType, TimeOnlyArg, IntArg);
            Assert.Equal(typeof(TimeOnly), func.ReturnType);
        }

        [Fact]
        public void AddDays_OnTimeOnly_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new AddDaysFunc(TimeOnlyArg, IntArg));
        }

        [Fact]
        public void AddHours_OnDateOnly_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new AddHoursFunc(DateOnlyArg, IntArg));
        }

        private static AbstractFunction CreateCalendarGetter(Type funcType, AbstractExpression argument) =>
            funcType.Name switch
            {
                nameof(YearFunc) => new YearFunc(argument),
                nameof(MonthFunc) => new MonthFunc(argument),
                nameof(DayFunc) => new DayFunc(argument),
                _ => throw new ArgumentOutOfRangeException(nameof(funcType)),
            };

        private static AbstractFunction CreateTimeGetter(Type funcType, AbstractExpression argument) =>
            funcType.Name switch
            {
                nameof(HourFunc) => new HourFunc(argument),
                nameof(MinuteFunc) => new MinuteFunc(argument),
                nameof(SecondFunc) => new SecondFunc(argument),
                _ => throw new ArgumentOutOfRangeException(nameof(funcType)),
            };

        private static AbstractFunction CreateAdd(Type funcType, AbstractExpression dateTime, AbstractExpression amount) =>
            funcType.Name switch
            {
                nameof(AddYearsFunc) => new AddYearsFunc(dateTime, amount),
                nameof(AddMonthsFunc) => new AddMonthsFunc(dateTime, amount),
                nameof(AddDaysFunc) => new AddDaysFunc(dateTime, amount),
                nameof(AddHoursFunc) => new AddHoursFunc(dateTime, amount),
                nameof(AddMinutesFunc) => new AddMinutesFunc(dateTime, amount),
                nameof(AddSecondsFunc) => new AddSecondsFunc(dateTime, amount),
                _ => throw new ArgumentOutOfRangeException(nameof(funcType)),
            };
    }
}
#endif
