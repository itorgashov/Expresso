using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class DateTimeAddFuncTests
    {
        private static readonly AbstractExpression DateTimeArg = new MockExpressionOfType(typeof(DateTime));
        private static readonly AbstractExpression IntArg = new MockExpressionOfType(typeof(int));
        private static readonly AbstractExpression InvalidArg = new MockExpressionOfType(typeof(string));

        [Theory]
        [InlineData(typeof(AddYearsFunc))]
        [InlineData(typeof(AddMonthsFunc))]
        [InlineData(typeof(AddDaysFunc))]
        [InlineData(typeof(AddHoursFunc))]
        [InlineData(typeof(AddMinutesFunc))]
        [InlineData(typeof(AddSecondsFunc))]
        public void DateTimeAdd_ValidArguments_ReturnsDateTime(Type funcType)
        {
            var func = CreateAdd(funcType, DateTimeArg, IntArg);
            Assert.Equal(2, func.Arguments.Count);
            Assert.Equal(typeof(DateTime), func.ReturnType);
        }

        [Theory]
        [InlineData(typeof(AddYearsFunc))]
        [InlineData(typeof(AddMonthsFunc))]
        [InlineData(typeof(AddDaysFunc))]
        [InlineData(typeof(AddHoursFunc))]
        [InlineData(typeof(AddMinutesFunc))]
        [InlineData(typeof(AddSecondsFunc))]
        public void DateTimeAdd_NegativeAmount_IsAllowed(Type funcType)
        {
            var func = CreateAdd(funcType, DateTimeArg, new Literal(-7));
            Assert.Equal(typeof(DateTime), func.ReturnType);
        }

        [Theory]
        [InlineData(typeof(AddYearsFunc))]
        [InlineData(typeof(AddMonthsFunc))]
        [InlineData(typeof(AddDaysFunc))]
        [InlineData(typeof(AddHoursFunc))]
        [InlineData(typeof(AddMinutesFunc))]
        [InlineData(typeof(AddSecondsFunc))]
        public void DateTimeAdd_ZeroAmount_IsAllowed(Type funcType)
        {
            var func = CreateAdd(funcType, DateTimeArg, new Literal(0));
            Assert.Equal(typeof(DateTime), func.ReturnType);
        }

        [Theory]
        [InlineData(typeof(AddYearsFunc))]
        [InlineData(typeof(AddMonthsFunc))]
        [InlineData(typeof(AddDaysFunc))]
        [InlineData(typeof(AddHoursFunc))]
        [InlineData(typeof(AddMinutesFunc))]
        [InlineData(typeof(AddSecondsFunc))]
        public void DateTimeAdd_NullDateTime_ThrowsArgumentNullException(Type funcType)
        {
            Assert.Throws<ArgumentNullException>(() => CreateAdd(funcType, null!, IntArg));
        }

        [Theory]
        [InlineData(typeof(AddYearsFunc))]
        [InlineData(typeof(AddMonthsFunc))]
        [InlineData(typeof(AddDaysFunc))]
        [InlineData(typeof(AddHoursFunc))]
        [InlineData(typeof(AddMinutesFunc))]
        [InlineData(typeof(AddSecondsFunc))]
        public void DateTimeAdd_NullAmount_ThrowsArgumentNullException(Type funcType)
        {
            Assert.Throws<ArgumentNullException>(() => CreateAdd(funcType, DateTimeArg, null!));
        }

        [Theory]
        [InlineData(typeof(AddYearsFunc))]
        [InlineData(typeof(AddMonthsFunc))]
        [InlineData(typeof(AddDaysFunc))]
        [InlineData(typeof(AddHoursFunc))]
        [InlineData(typeof(AddMinutesFunc))]
        [InlineData(typeof(AddSecondsFunc))]
        public void DateTimeAdd_InvalidDateTimeType_ThrowsArgumentException(Type funcType)
        {
            Assert.Throws<ArgumentException>(() => CreateAdd(funcType, InvalidArg, IntArg));
        }

        [Theory]
        [InlineData(typeof(AddYearsFunc))]
        [InlineData(typeof(AddMonthsFunc))]
        [InlineData(typeof(AddDaysFunc))]
        [InlineData(typeof(AddHoursFunc))]
        [InlineData(typeof(AddMinutesFunc))]
        [InlineData(typeof(AddSecondsFunc))]
        public void DateTimeAdd_InvalidAmountType_ThrowsArgumentException(Type funcType)
        {
            Assert.Throws<ArgumentException>(() => CreateAdd(funcType, DateTimeArg, InvalidArg));
        }

        [Fact]
        public void AddDaysFunc_DoubleAmount_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new AddDaysFunc(DateTimeArg, new MockExpressionOfType(typeof(double))));
        }

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
