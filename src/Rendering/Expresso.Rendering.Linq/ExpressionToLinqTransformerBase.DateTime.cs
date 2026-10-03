using System.Globalization;
using System.Linq.Expressions;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using V = Expresso.Core.CriteriaExpressions.IExpressoVisitor<Expresso.Rendering.Linq.LinqScope, Expresso.Rendering.Linq.LinqNode>;

namespace Expresso.Rendering.Linq
{
    public abstract partial class ExpressionToLinqTransformerBase
    {
        /// <summary>Calendar or clock component (<c>year</c> … <c>second</c>, <c>dayofyear</c>). Default: the matching BCL property.</summary>
        /// <param name="value">A <c>DateTime</c>, <c>DateOnly</c>, <c>TimeOnly</c> or time-of-day <c>TimeSpan</c> value.</param>
        /// <param name="property"><c>DateTime</c> property name (<c>Year</c>, <c>Hour</c>, <c>DayOfYear</c>, …).</param>
        protected virtual Expression DatePart(Expression value, string property) =>
            Expression.Property(value, value.Type == typeof(TimeSpan) ? property + "s" : property);

        /// <summary><c>dayofweek</c>, Sunday = 0. Default <c>(int)x.DayOfWeek</c>.</summary>
        protected virtual Expression DayOfWeek(Expression value) =>
            Expression.Convert(Expression.Property(value, nameof(DateTime.DayOfWeek)), typeof(int));

        /// <summary>
        /// <c>date</c> of a <c>DateTime</c>. Default <c>DateOnly.FromDateTime(x)</c> on .NET 6+, <c>x.Date</c> on netstandard2.0.
        /// </summary>
        protected virtual Expression Date(Expression value)
        {
#if NET6_0_OR_GREATER
            return Expression.Call(LinqEx.Method(typeof(DateOnly), nameof(DateOnly.FromDateTime), typeof(DateTime)), value);
#else
            return Expression.Property(value, nameof(DateTime.Date));
#endif
        }

        /// <summary>
        /// <c>time</c> of a <c>DateTime</c>. Default <c>TimeOnly.FromDateTime(x)</c> on .NET 6+, <c>x.TimeOfDay</c> on netstandard2.0.
        /// </summary>
        protected virtual Expression Time(Expression value)
        {
#if NET6_0_OR_GREATER
            return Expression.Call(LinqEx.Method(typeof(TimeOnly), nameof(TimeOnly.FromDateTime), typeof(DateTime)), value);
#else
            return Expression.Property(value, nameof(DateTime.TimeOfDay));
#endif
        }

        /// <summary>
        /// <c>add*</c>. Default: <c>AddYears</c> … <c>AddSeconds</c> on <c>DateTime</c>/<c>DateOnly</c>/<c>TimeOnly</c>
        /// (<c>TimeOnly</c> seconds via <c>Add(TimeSpan)</c>); <c>x + TimeSpan.FromX(n)</c> on a time-of-day <c>TimeSpan</c>.
        /// </summary>
        /// <param name="part">Unit.</param>
        /// <param name="value">Date/time value.</param>
        /// <param name="amount"><c>int</c> amount.</param>
        protected virtual Expression DateAdd(LinqDatePart part, Expression value, Expression amount)
        {
            if (value.Type == typeof(TimeSpan))
            {
                return Expression.Add(value, TimeSpanOf(part, amount));
            }

#if NET6_0_OR_GREATER
            if (value.Type == typeof(TimeOnly) && part == LinqDatePart.Second)
            {
                return Expression.Call(value, LinqEx.Method(typeof(TimeOnly), nameof(TimeOnly.Add), typeof(TimeSpan)), TimeSpanOf(part, amount));
            }
#endif
            var method = "Add" + part + "s";
            var byInt = value.Type.GetMethod(method, new[] { typeof(int) });
            return byInt is not null && byInt.GetParameters()[0].ParameterType == typeof(int)
                ? Expression.Call(value, byInt, amount)
                : Expression.Call(value, LinqEx.Method(value.Type, method, typeof(double)), Expression.Convert(amount, typeof(double)));
        }

        /// <summary><c>TimeSpan.FromHours/FromMinutes/FromSeconds((double)amount)</c>.</summary>
        protected static Expression TimeSpanOf(LinqDatePart part, Expression amount) =>
            Expression.Call(LinqEx.Method(typeof(TimeSpan), "From" + part + "s", typeof(double)), Expression.Convert(amount, typeof(double)));

        private LinqNode Part(AbstractFunction node, LinqScope s, string property) =>
            Propagate(a => DatePart(a[0], property), Visit(node.Arguments[0], s));

        private LinqNode AddPart(DateTimeAddFunction node, LinqDatePart part, LinqScope s) =>
            Propagate(a => DateAdd(part, a[0], a[1]), Visit(node.Arguments[0], s), Visit(node.Arguments[1], s));

        private LinqNode Cast(AbstractFunction node, LinqScope s, Func<string, object> parse, Func<Expression, Expression> convert)
        {
            if (node.Arguments[0] is Literal { Value: string text })
            {
                return Parameter(parse(text));
            }

            if (node.Arguments[0].ReturnType == typeof(string))
            {
                throw new NotSupportedException($"{node.GetType().Name} accepts a string argument only as a literal in LINQ rendering.");
            }

            var arg = Visit(node.Arguments[0], s);
            return arg.Type == typeof(DateTime) ? Propagate(a => convert(a[0]), arg) : arg;
        }

        LinqNode V.VisitYear(YearFunc node, LinqScope s) => Part(node, s, nameof(DateTime.Year));
        LinqNode V.VisitMonth(MonthFunc node, LinqScope s) => Part(node, s, nameof(DateTime.Month));
        LinqNode V.VisitDay(DayFunc node, LinqScope s) => Part(node, s, nameof(DateTime.Day));
        LinqNode V.VisitDayOfYear(DayOfYearFunc node, LinqScope s) => Part(node, s, nameof(DateTime.DayOfYear));
        LinqNode V.VisitHour(HourFunc node, LinqScope s) => Part(node, s, nameof(DateTime.Hour));
        LinqNode V.VisitMinute(MinuteFunc node, LinqScope s) => Part(node, s, nameof(DateTime.Minute));
        LinqNode V.VisitSecond(SecondFunc node, LinqScope s) => Part(node, s, nameof(DateTime.Second));
        LinqNode V.VisitDayOfWeek(DayOfWeekFunc node, LinqScope s) => Propagate(a => DayOfWeek(a[0]), Visit(node.Arguments[0], s));

        LinqNode V.VisitDate(DateFunc node, LinqScope s) =>
#if NET6_0_OR_GREATER
            Cast(node, s, t => DateOnly.FromDateTime(DateTime.Parse(t, CultureInfo.InvariantCulture)), Date);
#else
            Cast(node, s, t => DateTime.Parse(t, CultureInfo.InvariantCulture).Date, Date);
#endif

        LinqNode V.VisitTime(TimeFunc node, LinqScope s) =>
#if NET6_0_OR_GREATER
            Cast(node, s, t => TimeOnly.Parse(t, CultureInfo.InvariantCulture), Time);
#else
            Cast(node, s, t => TimeSpan.Parse(t, CultureInfo.InvariantCulture), Time);
#endif

        LinqNode V.VisitAddYears(AddYearsFunc node, LinqScope s) => AddPart(node, LinqDatePart.Year, s);
        LinqNode V.VisitAddMonths(AddMonthsFunc node, LinqScope s) => AddPart(node, LinqDatePart.Month, s);
        LinqNode V.VisitAddDays(AddDaysFunc node, LinqScope s) => AddPart(node, LinqDatePart.Day, s);
        LinqNode V.VisitAddHours(AddHoursFunc node, LinqScope s) => AddPart(node, LinqDatePart.Hour, s);
        LinqNode V.VisitAddMinutes(AddMinutesFunc node, LinqScope s) => AddPart(node, LinqDatePart.Minute, s);
        LinqNode V.VisitAddSeconds(AddSecondsFunc node, LinqScope s) => AddPart(node, LinqDatePart.Second, s);
    }
}
