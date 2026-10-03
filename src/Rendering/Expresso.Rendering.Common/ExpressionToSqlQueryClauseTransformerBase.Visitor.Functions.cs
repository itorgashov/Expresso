using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using System.Text;
using V = Expresso.Core.CriteriaExpressions.IExpressoVisitor<Expresso.Rendering.SqlRenderScope, System.Text.StringBuilder>;

namespace Expresso.Rendering
{
    public abstract partial class ExpressionToSqlQueryClauseTransformerBase
    {
        private StringBuilder Like(AbstractFunction node, LikePatternKind kind, SqlRenderScope s)
        {
            GenerateLikeClause(node.Arguments[0], node.Arguments[1], kind, s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        private StringBuilder DatePart(string part, AbstractFunction node, SqlRenderScope s)
        {
            AppendDatePart(part, node.Arguments[0], s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        private StringBuilder DateAdd(string part, DateTimeAddFunction node, SqlRenderScope s)
        {
            AppendDateAdd(part, node, s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitStrStartswith(StrStartswithFunc node, SqlRenderScope s) => Like(node, LikePatternKind.Prefix, s);
        StringBuilder V.VisitStrEndswith(StrEndswithFunc node, SqlRenderScope s) => Like(node, LikePatternKind.Suffix, s);
        StringBuilder V.VisitStrContains(StrContainsFunc node, SqlRenderScope s) => Like(node, LikePatternKind.Contains, s);

        StringBuilder V.VisitSubString(SubStringFunc node, SqlRenderScope s)
        {
            AppendSubstring(node, s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitLeft(LeftFunc node, SqlRenderScope s)
        {
            AppendLeft(node, s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitRight(RightFunc node, SqlRenderScope s)
        {
            AppendRight(node, s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitConcat(ConcatFunc node, SqlRenderScope s)
        {
            AppendConcat(node, s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitLower(LowerFunc node, SqlRenderScope s) => Named("LOWER", node.Arguments, s);
        StringBuilder V.VisitUpper(UpperFunc node, SqlRenderScope s) => Named("UPPER", node.Arguments, s);
        StringBuilder V.VisitTrim(TrimFunc node, SqlRenderScope s) => Named("TRIM", node.Arguments, s);
        StringBuilder V.VisitLTrim(LTrimFunc node, SqlRenderScope s) => Named("LTRIM", node.Arguments, s);
        StringBuilder V.VisitRTrim(RTrimFunc node, SqlRenderScope s) => Named("RTRIM", node.Arguments, s);
        StringBuilder V.VisitReplace(ReplaceFunc node, SqlRenderScope s) => Named("REPLACE", node.Arguments, s);
        StringBuilder V.VisitLen(LenFunc node, SqlRenderScope s) => Named(LengthFunctionName, node.Arguments, s);

        StringBuilder V.VisitIndexOf(IndexOfFunc node, SqlRenderScope s)
        {
            AppendIndexOf(node, s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitYear(YearFunc node, SqlRenderScope s)
        {
            AppendYear(node.Arguments[0], s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitMonth(MonthFunc node, SqlRenderScope s)
        {
            AppendMonth(node.Arguments[0], s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitDay(DayFunc node, SqlRenderScope s)
        {
            AppendDay(node.Arguments[0], s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitDayOfYear(DayOfYearFunc node, SqlRenderScope s) => DatePart("dayofyear", node, s);
        StringBuilder V.VisitHour(HourFunc node, SqlRenderScope s) => DatePart("hour", node, s);
        StringBuilder V.VisitMinute(MinuteFunc node, SqlRenderScope s) => DatePart("minute", node, s);
        StringBuilder V.VisitSecond(SecondFunc node, SqlRenderScope s) => DatePart("second", node, s);

        StringBuilder V.VisitDayOfWeek(DayOfWeekFunc node, SqlRenderScope s)
        {
            AppendDayOfWeek(node.Arguments[0], s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitDate(DateFunc node, SqlRenderScope s)
        {
            AppendDateCast(node.Arguments[0], s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitTime(TimeFunc node, SqlRenderScope s)
        {
            AppendTimeCast(node.Arguments[0], s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitAddYears(AddYearsFunc node, SqlRenderScope s) => DateAdd("year", node, s);
        StringBuilder V.VisitAddMonths(AddMonthsFunc node, SqlRenderScope s) => DateAdd("month", node, s);
        StringBuilder V.VisitAddDays(AddDaysFunc node, SqlRenderScope s) => DateAdd("day", node, s);
        StringBuilder V.VisitAddHours(AddHoursFunc node, SqlRenderScope s) => DateAdd("hour", node, s);
        StringBuilder V.VisitAddMinutes(AddMinutesFunc node, SqlRenderScope s) => DateAdd("minute", node, s);
        StringBuilder V.VisitAddSeconds(AddSecondsFunc node, SqlRenderScope s) => DateAdd("second", node, s);
    }
}
