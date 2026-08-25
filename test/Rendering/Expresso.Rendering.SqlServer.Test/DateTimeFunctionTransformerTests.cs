using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.SqlServer;

namespace Expresso.Tests.SqlServer
{
    public class DateTimeFunctionTransformerTests
    {
        private readonly ExpressionToSqlServerQueryClauseTransformer _transformer = new();

        private readonly Dictionary<string, string> _fieldMap = new()
        {
            { "createdat", "b.created_at" },
        };

        private const string ParamPrefix = "param";

        [Fact]
        public void GenerateWhereClause_Year_ReturnsYearSql()
        {
            var result = RenderEqInt(new YearFunc(CreatedAt()));

            Assert.Equal("(YEAR([b].[created_at]) = @param_0)", result.whereClause);
            Assert.Equal(2020, result.parameters["@param_0"]);
        }

        [Fact]
        public void GenerateWhereClause_Month_ReturnsMonthSql()
        {
            var result = RenderEqInt(new MonthFunc(CreatedAt()));
            Assert.Equal("(MONTH([b].[created_at]) = @param_0)", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_Day_ReturnsDaySql()
        {
            var result = RenderEqInt(new DayFunc(CreatedAt()));
            Assert.Equal("(DAY([b].[created_at]) = @param_0)", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_DayOfYear_ReturnsDatePartSql()
        {
            var result = RenderEqInt(new DayOfYearFunc(CreatedAt()));
            Assert.Equal("(DATEPART(dayofyear, [b].[created_at]) = @param_0)", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_Hour_ReturnsDatePartSql()
        {
            var result = RenderEqInt(new HourFunc(CreatedAt()));
            Assert.Equal("(DATEPART(hour, [b].[created_at]) = @param_0)", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_Minute_ReturnsDatePartSql()
        {
            var result = RenderEqInt(new MinuteFunc(CreatedAt()));
            Assert.Equal("(DATEPART(minute, [b].[created_at]) = @param_0)", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_Second_ReturnsDatePartSql()
        {
            var result = RenderEqInt(new SecondFunc(CreatedAt()));
            Assert.Equal("(DATEPART(second, [b].[created_at]) = @param_0)", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_Date_ReturnsCastSql()
        {
            FilterCriteria filter = new()
            {
                Expression = new EqFunc(
                    new DateFunc(CreatedAt()),
                    new Literal(new DateTime(2020, 1, 1)))
            };

            var result = _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);

            Assert.Equal("(CAST([b].[created_at] AS date) = @param_0)", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_DayOfWeek_ReturnsDateFirstFormula()
        {
            var result = RenderEqInt(new DayOfWeekFunc(CreatedAt()), 0);

            Assert.Equal("(((DATEPART(weekday, [b].[created_at]) + @@DATEFIRST - 1) % 7) = @param_0)", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_AddYears_ReturnsDateAddSql()
        {
            var result = RenderAddCompare(new AddYearsFunc(CreatedAt(), new Literal(1)));
            Assert.Equal("(DATEADD(year, @param_0, [b].[created_at]) = [b].[created_at])", result.whereClause);
            Assert.Equal(1, result.parameters["@param_0"]);
        }

        [Fact]
        public void GenerateWhereClause_AddMonthsZero_ReturnsDateAddSql()
        {
            var result = RenderAddCompare(new AddMonthsFunc(CreatedAt(), new Literal(0)));
            Assert.Equal("(DATEADD(month, @param_0, [b].[created_at]) = [b].[created_at])", result.whereClause);
            Assert.Equal(0, result.parameters["@param_0"]);
        }

        [Fact]
        public void GenerateWhereClause_AddDaysNegative_ReturnsDateAddSql()
        {
            FilterCriteria filter = new()
            {
                Expression = new GtFunc(
                    new AddDaysFunc(CreatedAt(), new Literal(-7)),
                    CreatedAt())
            };

            var result = _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);

            Assert.Equal("(DATEADD(day, @param_0, [b].[created_at]) > [b].[created_at])", result.whereClause);
            Assert.Equal(-7, result.parameters["@param_0"]);
        }

        [Fact]
        public void GenerateWhereClause_AddHours_ReturnsDateAddSql()
        {
            var result = RenderAddCompare(new AddHoursFunc(CreatedAt(), new Literal(24)));
            Assert.Equal("(DATEADD(hour, @param_0, [b].[created_at]) = [b].[created_at])", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_AddMinutes_ReturnsDateAddSql()
        {
            var result = RenderAddCompare(new AddMinutesFunc(CreatedAt(), new Literal(-30)));
            Assert.Equal("(DATEADD(minute, @param_0, [b].[created_at]) = [b].[created_at])", result.whereClause);
            Assert.Equal(-30, result.parameters["@param_0"]);
        }

        [Fact]
        public void GenerateWhereClause_AddSeconds_ReturnsDateAddSql()
        {
            var result = RenderAddCompare(new AddSecondsFunc(CreatedAt(), new Literal(0)));
            Assert.Equal("(DATEADD(second, @param_0, [b].[created_at]) = [b].[created_at])", result.whereClause);
        }

        [Fact]
        public void GenerateOrderByClause_Year_ReturnsYearSql()
        {
            var sort = new SortDirective(
            [
                new SortDirectiveItem
                {
                    Expression = new YearFunc(CreatedAt()),
                    Direction = SortDirection.Ascending,
                },
            ]);

            var result = _transformer.RenderOrderByClause(sort, _fieldMap, ParamPrefix);

            Assert.Equal("YEAR([b].[created_at]) ASC", result.orderByClause);
        }

        [Fact]
        public void GenerateOrderByClause_AddDays_ReturnsDateAddSql()
        {
            var sort = new SortDirective(
            [
                new SortDirectiveItem
                {
                    Expression = new AddDaysFunc(CreatedAt(), new Literal(1)),
                    Direction = SortDirection.Descending,
                },
            ]);

            var result = _transformer.RenderOrderByClause(sort, _fieldMap, ParamPrefix);

            Assert.Equal("DATEADD(day, @param_0, [b].[created_at]) DESC", result.orderByClause);
            Assert.Equal(1, result.parameters["@param_0"]);
        }

        private static Field CreatedAt() => new("createdat", typeof(DateTime));

        private (string whereClause, Dictionary<string, object> parameters) RenderEqInt(AbstractExpression left, int right = 2020)
        {
            FilterCriteria filter = new()
            {
                Expression = new EqFunc(left, new Literal(right))
            };
            return _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);
        }

        private (string whereClause, Dictionary<string, object> parameters) RenderAddCompare(DateTimeAddFunction addFunc)
        {
            FilterCriteria filter = new()
            {
                Expression = new EqFunc(addFunc, CreatedAt())
            };
            return _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);
        }
    }
}
