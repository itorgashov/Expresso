using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using System.Text;

namespace Expresso.SqlServer
{
    public partial class ExpressionToSqlServerQueryClauseTransformer
    {
        private bool TryGenerateDateTimeFunction(
            AbstractExpression expression,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix)
        {
            switch (expression)
            {
                case YearFunc year:
                    GenerateNamedFunction("YEAR", year.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case MonthFunc month:
                    GenerateNamedFunction("MONTH", month.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case DayFunc day:
                    GenerateNamedFunction("DAY", day.Arguments, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case DayOfYearFunc dayOfYear:
                    GenerateDatePartClause("dayofyear", dayOfYear.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case HourFunc hour:
                    GenerateDatePartClause("hour", hour.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case MinuteFunc minute:
                    GenerateDatePartClause("minute", minute.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case SecondFunc second:
                    GenerateDatePartClause("second", second.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case DayOfWeekFunc dayOfWeek:
                    GenerateDayOfWeekClause(dayOfWeek.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case DateFunc date:
                    GenerateDateClause(date.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case AddYearsFunc addYears:
                    GenerateDateAddClause("year", addYears, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case AddMonthsFunc addMonths:
                    GenerateDateAddClause("month", addMonths, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case AddDaysFunc addDays:
                    GenerateDateAddClause("day", addDays, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case AddHoursFunc addHours:
                    GenerateDateAddClause("hour", addHours, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case AddMinutesFunc addMinutes:
                    GenerateDateAddClause("minute", addMinutes, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                case AddSecondsFunc addSeconds:
                    GenerateDateAddClause("second", addSeconds, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
                    return true;
                default:
                    return false;
            }
        }

        private void GenerateDatePartClause(
            string part,
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix)
        {
            sqlBuilder.Append("DATEPART(");
            sqlBuilder.Append(part);
            sqlBuilder.Append(", ");
            GenerateClause(argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(')');
        }

        // Maps to C# DayOfWeek (Sunday=0 … Saturday=6) when SQL Server DATEFIRST is 7 (default).
        private void GenerateDayOfWeekClause(
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix)
        {
            sqlBuilder.Append("((DATEPART(weekday, ");
            GenerateClause(argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(") + @@DATEFIRST - 1) % 7)");
        }

        private void GenerateDateClause(
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix)
        {
            sqlBuilder.Append("CAST(");
            GenerateClause(argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(" AS date)");
        }

        private void GenerateDateAddClause(
            string datePart,
            DateTimeAddFunction addFunction,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix)
        {
            sqlBuilder.Append("DATEADD(");
            sqlBuilder.Append(datePart);
            sqlBuilder.Append(", ");
            GenerateClause(addFunction.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(", ");
            GenerateClause(addFunction.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix);
            sqlBuilder.Append(')');
        }
    }
}
