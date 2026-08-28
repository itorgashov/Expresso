using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Parsing
{
    internal sealed partial class ExpressionParser
    {
        private static bool TryCreateDateTimeFunction(string functionName, List<AbstractExpression> arguments, out AbstractExpression result)
        {
            result = functionName.ToLowerInvariant() switch
            {
                "year" => CreateYear(arguments),
                "month" => CreateMonth(arguments),
                "day" => CreateDay(arguments),
                "dayofyear" => CreateDayOfYear(arguments),
                "hour" => CreateHour(arguments),
                "minute" => CreateMinute(arguments),
                "second" => CreateSecond(arguments),
                "dayofweek" => CreateDayOfWeek(arguments),
                "date" => CreateDate(arguments),
#if NET6_0_OR_GREATER
                "time" => CreateTime(arguments),
#endif
                "addyears" => CreateAddYears(arguments),
                "addmonths" => CreateAddMonths(arguments),
                "adddays" => CreateAddDays(arguments),
                "addhours" => CreateAddHours(arguments),
                "addminutes" => CreateAddMinutes(arguments),
                "addseconds" => CreateAddSeconds(arguments),
                _ => null!
            };

            return result is not null;
        }

        private static AbstractExpression CreateYear(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Year() function should have 1 argument.");
            return new YearFunc(CoerceToDateTime(arguments[0]));
        }

        private static AbstractExpression CreateMonth(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Month() function should have 1 argument.");
            return new MonthFunc(CoerceToDateTime(arguments[0]));
        }

        private static AbstractExpression CreateDay(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Day() function should have 1 argument.");
            return new DayFunc(CoerceToDateTime(arguments[0]));
        }

        private static AbstractExpression CreateDayOfYear(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Dayofyear() function should have 1 argument.");
            return new DayOfYearFunc(CoerceToDateTime(arguments[0]));
        }

        private static AbstractExpression CreateHour(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Hour() function should have 1 argument.");
            return new HourFunc(CoerceToDateTime(arguments[0]));
        }

        private static AbstractExpression CreateMinute(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Minute() function should have 1 argument.");
            return new MinuteFunc(CoerceToDateTime(arguments[0]));
        }

        private static AbstractExpression CreateSecond(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Second() function should have 1 argument.");
            return new SecondFunc(CoerceToDateTime(arguments[0]));
        }

        private static AbstractExpression CreateDayOfWeek(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Dayofweek() function should have 1 argument.");
            return new DayOfWeekFunc(CoerceToDateTime(arguments[0]));
        }

        private static AbstractExpression CreateDate(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Date() function should have 1 argument.");
            return new DateFunc(CoerceToString(arguments[0]));
        }

#if NET6_0_OR_GREATER
        private static AbstractExpression CreateTime(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 1, "Time() function should have 1 argument.");
            return new TimeFunc(CoerceToString(arguments[0]));
        }
#endif

        private static AbstractExpression CreateAddYears(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 2, "Addyears() function should have 2 arguments.");
            return new AddYearsFunc(CoerceToDateTime(arguments[0]), CoerceToInt(arguments[1]));
        }

        private static AbstractExpression CreateAddMonths(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 2, "Addmonths() function should have 2 arguments.");
            return new AddMonthsFunc(CoerceToDateTime(arguments[0]), CoerceToInt(arguments[1]));
        }

        private static AbstractExpression CreateAddDays(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 2, "Adddays() function should have 2 arguments.");
            return new AddDaysFunc(CoerceToDateTime(arguments[0]), CoerceToInt(arguments[1]));
        }

        private static AbstractExpression CreateAddHours(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 2, "Addhours() function should have 2 arguments.");
            return new AddHoursFunc(CoerceToDateTime(arguments[0]), CoerceToInt(arguments[1]));
        }

        private static AbstractExpression CreateAddMinutes(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 2, "Addminutes() function should have 2 arguments.");
            return new AddMinutesFunc(CoerceToDateTime(arguments[0]), CoerceToInt(arguments[1]));
        }

        private static AbstractExpression CreateAddSeconds(List<AbstractExpression> arguments)
        {
            RequireCount(arguments, 2, "Addseconds() function should have 2 arguments.");
            return new AddSecondsFunc(CoerceToDateTime(arguments[0]), CoerceToInt(arguments[1]));
        }

        private static AbstractExpression CoerceToDateTime(AbstractExpression expression)
        {
            return expression is StringLiteral stringLiteral
                ? CreateLiteral(stringLiteral.Value, typeof(DateTime))
                : expression;
        }
    }
}
