using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using System.Text;

namespace Expresso.Rendering
{
    public abstract partial class ExpressionToSqlQueryClauseTransformerBase
    {
        protected virtual bool TryGenerateDateTimeFunction(
            AbstractExpression expression,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            switch (expression)
            {
                case YearFunc year:
                    AppendYear(year.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case MonthFunc month:
                    AppendMonth(month.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case DayFunc day:
                    AppendDay(day.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case DayOfYearFunc dayOfYear:
                    AppendDatePart("dayofyear", dayOfYear.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case HourFunc hour:
                    AppendDatePart("hour", hour.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case MinuteFunc minute:
                    AppendDatePart("minute", minute.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case SecondFunc second:
                    AppendDatePart("second", second.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case DayOfWeekFunc dayOfWeek:
                    AppendDayOfWeek(dayOfWeek.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case DateFunc date:
                    AppendDateCast(date.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case TimeFunc time:
                    AppendTimeCast(time.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case AddYearsFunc addYears:
                    AppendDateAdd("year", addYears, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case AddMonthsFunc addMonths:
                    AppendDateAdd("month", addMonths, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case AddDaysFunc addDays:
                    AppendDateAdd("day", addDays, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case AddHoursFunc addHours:
                    AppendDateAdd("hour", addHours, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case AddMinutesFunc addMinutes:
                    AppendDateAdd("minute", addMinutes, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case AddSecondsFunc addSeconds:
                    AppendDateAdd("second", addSeconds, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                default:
                    return false;
            }
        }

        protected virtual void AppendYear(
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            GenerateNamedFunction("YEAR", new[] { argument }, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
        }

        protected virtual void AppendMonth(
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            GenerateNamedFunction("MONTH", new[] { argument }, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
        }

        protected virtual void AppendDay(
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            GenerateNamedFunction("DAY", new[] { argument }, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
        }

        protected virtual void AppendDatePart(
            string part,
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("DATEPART(");
            sqlBuilder.Append(part);
            sqlBuilder.Append(", ");
            GenerateClause(argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(')');
        }

        protected virtual void AppendDayOfWeek(
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("((DATEPART(weekday, ");
            GenerateClause(argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(") + @@DATEFIRST - 1) % 7)");
        }

        protected virtual void AppendDateCast(
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("CAST(");
            GenerateClause(argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(" AS date)");
        }

        protected virtual void AppendTimeCast(
            AbstractExpression argument,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("CAST(");
            GenerateClause(argument, fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(" AS time)");
        }

        protected virtual void AppendDateAdd(
            string datePart,
            DateTimeAddFunction addFunction,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            sqlBuilder.Append("DATEADD(");
            sqlBuilder.Append(datePart);
            sqlBuilder.Append(", ");
            GenerateClause(addFunction.Arguments[1], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(", ");
            GenerateClause(addFunction.Arguments[0], fieldToColumnMap, sqlBuilder, parameters, paramNamePrefix, collections);
            sqlBuilder.Append(')');
        }
    }
}
