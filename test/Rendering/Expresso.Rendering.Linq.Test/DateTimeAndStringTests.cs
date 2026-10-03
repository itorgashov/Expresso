using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using static Expresso.Rendering.Linq.Test.TestRows;

namespace Expresso.Rendering.Linq.Test
{
    /// <summary>Date/time and string functions over nullable and time-of-day values, on both profiles.</summary>
    public class DateTimeAndStringTests
    {
        private static List<Row> Rows() => new()
        {
            new Row
            {
                Id = 1,
                When = new DateTime(2024, 2, 29, 23, 30, 15),
                Clock = new TimeSpan(23, 0, 0),
                Text = " Ab ",
#if NET6_0_OR_GREATER
                Day = new DateOnly(2024, 1, 31),
                At = new TimeOnly(23, 59, 50),
#endif
            },
            new Row
            {
                Id = 2,
                When = null,
                Clock = new TimeSpan(0, 30, 0),
                Text = null,
#if NET6_0_OR_GREATER
                Day = new DateOnly(2023, 6, 15),
                At = new TimeOnly(12, 0),
#endif
            },
        };

        private static void AssertBoth(BooleanFunction filter, params int[] expected)
        {
            Assert.Equal(expected, Ids(Rows(), filter));
            Assert.Equal(expected, QueryableIds(Rows(), filter));
        }

        [Fact]
        public void NullableDateTime_GettersAreUnknownForNull()
        {
            AssertBoth(Eq(new YearFunc(When), 2024), 1);
            AssertBoth(Eq(new DayOfYearFunc(When), 60), 1);
            AssertBoth(Eq(new DayOfWeekFunc(When), (int)DayOfWeek.Thursday), 1);
            AssertBoth(Eq(new SecondFunc(When), 15), 1);
            AssertBoth(new NotFunc(Eq(new MonthFunc(When), 1)), 1);
            AssertBoth(Eq(new MonthFunc(new AddYearsFunc(When, L(1))), 2), 1);
            AssertBoth(Eq(new DayFunc(new AddMonthsFunc(When, L(1))), 29), 1);
        }

        [Fact]
        public void TimeSpanClock_GettersUseComponents()
        {
            AssertBoth(Eq(new HourFunc(Clock), 23), 1);
            AssertBoth(Eq(new MinuteFunc(Clock), 30), 2);
            AssertBoth(Eq(new SecondFunc(Clock), 0), 1, 2);
        }

        [Fact]
        public void TimeSpanClock_AddWrapsInMemory()
        {
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(new HourFunc(new AddHoursFunc(Clock, L(2))), 1)));
            Assert.Equal(new[] { 2 }, Ids(Rows(), Eq(new HourFunc(new AddMinutesFunc(Clock, L(-60))), 23)));
            Assert.Equal(new[] { 1 }, Ids(Rows(), new EqFunc(new AddSecondsFunc(Clock, L(3600)), L(TimeSpan.Zero))));
        }

        [Fact]
        public void NullString_FunctionsPropagateNull()
        {
            AssertBoth(Eq(new TrimFunc(Text), "Ab"), 1);
            AssertBoth(Eq(new LTrimFunc(Text), "Ab "), 1);
            AssertBoth(Eq(new RTrimFunc(Text), " Ab"), 1);
            AssertBoth(Eq(new LowerFunc(Text), " ab "), 1);
            AssertBoth(Eq(new UpperFunc(Text), " AB "), 1);
            AssertBoth(Eq(new LenFunc(Text), 4), 1);
            AssertBoth(Eq(new ReplaceFunc(Text, L("A"), L("x")), " xb "), 1);
            AssertBoth(Eq(new LeftFunc(Text, L(2)), " A"), 1);
            AssertBoth(Eq(new RightFunc(Text, L(9)), " Ab "), 1);
            AssertBoth(Eq(new IndexOfFunc(Text, L("b")), 2), 1);
            AssertBoth(new StrEndswithFunc(Text, L("b ")), 1);
            Assert.Equal(new[] { 2 }, QueryableIds(Rows(), new IsNullFunc(new ConcatFunc(new List<AbstractExpression> { L("x"), Text }))));
        }

        [Fact]
        public void InMemoryConcat_TreatsNullAsEmpty()
        {
            var concat = new ConcatFunc(new List<AbstractExpression> { L("x"), Text, L("y") });

            Assert.Empty(Ids(Rows(), new IsNullFunc(concat)));
            Assert.Equal(new[] { 2 }, Ids(Rows(), Eq(concat, "xy")));
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(concat, "x Ab y")));
        }

        [Fact]
        public void InMemoryStrings_AreOrdinal()
        {
            Assert.Empty(Ids(Rows(), new StrStartswithFunc(Text, L(" a"))));
            Assert.Empty(Ids(Rows(), new StrContainsFunc(Text, L("AB"))));
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(new SubStringFunc(Text, L(0), L(3)), " A")));
        }

#if NET6_0_OR_GREATER
        [Fact]
        public void DateOnlyAndTimeOnly_UseTheirMembers()
        {
            var day = new Field("day", typeof(DateOnly));
            var at = new Field("at", typeof(TimeOnly));
            AssertBoth(Eq(new MonthFunc(new AddMonthsFunc(day, L(1))), 2), 1);
            AssertBoth(Eq(new DayFunc(new AddDaysFunc(day, L(1))), 1), 1);
            AssertBoth(Eq(new YearFunc(new AddYearsFunc(day, L(1))), 2025), 1);
            AssertBoth(new EqFunc(new DateFunc(day), L(new DateOnly(2024, 1, 31))), 1);
            AssertBoth(Eq(new HourFunc(new AddSecondsFunc(at, L(10))), 0), 1);
            AssertBoth(Eq(new MinuteFunc(new AddMinutesFunc(at, L(1))), 0), 1);
            AssertBoth(Eq(new HourFunc(new AddHoursFunc(at, L(1))), 0), 1);
            AssertBoth(new EqFunc(new TimeFunc(at), L(new TimeOnly(23, 59, 50))), 1);
            AssertBoth(new EqFunc(new DateFunc(When), L(new DateOnly(2024, 2, 29))), 1);
            AssertBoth(new EqFunc(new TimeFunc(When), L(new TimeOnly(23, 30, 15))), 1);
        }
#else
        [Fact]
        public void DateAndTime_OfDateTime_OnNetStandard()
        {
            AssertBoth(new EqFunc(new DateFunc(When), L(new DateTime(2024, 2, 29))), 1);
            AssertBoth(new EqFunc(new TimeFunc(When), L(new TimeSpan(23, 30, 15))), 1);
            AssertBoth(new EqFunc(new TimeFunc(Clock), L(new TimeSpan(0, 30, 0))), 2);
        }
#endif
    }
}
