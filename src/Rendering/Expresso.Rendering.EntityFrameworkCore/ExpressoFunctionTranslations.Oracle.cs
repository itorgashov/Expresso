using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Translation = System.Func<System.Collections.Generic.IReadOnlyList<Microsoft.EntityFrameworkCore.Query.SqlExpressions.SqlExpression>, Microsoft.EntityFrameworkCore.Query.SqlExpressions.SqlExpression>;

namespace Expresso.Rendering.EntityFrameworkCore
{
    internal static partial class ExpressoFunctionTranslations
    {
        private static IEnumerable<(string Marker, Type Value, Translation Translation)> OracleTemporalEntries()
        {
            Translation DateNumber(string format) => a =>
            {
                RejectOracleText(a[0]);
                return Function("TO_NUMBER", typeof(int), OracleNumber, Function("TO_CHAR", typeof(string), OracleText, a[0], Fragment("'" + format + "'")));
            };

            Translation TimeNumber(string intervalPattern, int group, string dateFormat) => a =>
            {
                RejectOracleText(a[0]);
                var store = a[0].TypeMapping?.StoreType ?? string.Empty;
                var dateColumn = store.IndexOf("INTERVAL", StringComparison.OrdinalIgnoreCase) < 0
                    && (store.IndexOf("DATE", StringComparison.OrdinalIgnoreCase) >= 0 || store.IndexOf("TIMESTAMP", StringComparison.OrdinalIgnoreCase) >= 0);
                if (dateColumn)
                {
                    return Function("TO_NUMBER", typeof(int), OracleNumber, Function("TO_CHAR", typeof(string), OracleText, a[0], Fragment("'" + dateFormat + "'")));
                }

                var text = Function("TO_CHAR", typeof(string), OracleText, a[0]);
                var magnitude = Function(
                    "TO_NUMBER",
                    typeof(int),
                    OracleNumber,
                    Function(
                        "REGEXP_SUBSTR",
                        typeof(string),
                        OracleText,
                        text,
                        Fragment("'" + intervalPattern + "'"),
                        Fragment("1"),
                        Fragment("1"),
                        Fragment("NULL"),
                        Fragment(group.ToString())));
                var negative = new SqlBinaryExpression(
                    ExpressionType.Equal,
                    Function("SUBSTR", typeof(string), OracleText, text, Fragment("1"), Fragment("1")),
                    Fragment("'-'"),
                    typeof(bool),
                    OracleBool);
                var sign = new CaseExpression(
                    new[] { new CaseWhenClause(negative, new SqlConstantExpression(Expression.Constant(-1), OracleNumber)) },
                    new SqlConstantExpression(Expression.Constant(1), OracleNumber));
                return new SqlBinaryExpression(ExpressionType.Multiply, magnitude, sign, typeof(int), OracleNumber);
            };

            Translation AddYearMonth(string unit) => a =>
            {
                RejectOracleText(a[0]);
                var mapping = DateMapping(a[0]);
                return new SqlBinaryExpression(
                    ExpressionType.Add,
                    a[0],
                    Function("NUMTOYMINTERVAL", a[0].Type, mapping, a[1], Fragment("'" + unit + "'")),
                    a[0].Type,
                    mapping);
            };

            Translation AddInterval(string unit) => a =>
            {
                RejectOracleText(a[0]);
                var mapping = IntervalMapping(a[0]);
                return new SqlBinaryExpression(
                    ExpressionType.Add,
                    a[0],
                    Function("NUMTODSINTERVAL", a[0].Type, mapping, a[1], Fragment("'" + unit + "'")),
                    a[0].Type,
                    mapping);
            };

            return new (string, Type, Translation)[]
            {
                (nameof(ExpressoDbFunctions.Year), DateOnlyType, DateNumber("YYYY")),
                (nameof(ExpressoDbFunctions.Month), DateOnlyType, DateNumber("MM")),
                (nameof(ExpressoDbFunctions.Day), DateOnlyType, DateNumber("DD")),
                (nameof(ExpressoDbFunctions.DayOfYear), DateOnlyType, DateNumber("DDD")),
                (nameof(ExpressoDbFunctions.AddYears), DateOnlyType, AddYearMonth("YEAR")),
                (nameof(ExpressoDbFunctions.AddMonths), DateOnlyType, AddYearMonth("MONTH")),
                (nameof(ExpressoDbFunctions.AddDays), DateOnlyType, a =>
                {
                    RejectOracleText(a[0]);
                    var mapping = DateMapping(a[0]);
                    return new SqlBinaryExpression(ExpressionType.Add, a[0], a[1], typeof(DateOnly), mapping);
                }),
                (nameof(ExpressoDbFunctions.Hour), TimeOnlyType, TimeNumber(" ([0-9]{2}):", 1, "HH24")),
                (nameof(ExpressoDbFunctions.Minute), TimeOnlyType, TimeNumber(" ([0-9]{2}):([0-9]{2}):", 2, "MI")),
                (nameof(ExpressoDbFunctions.Second), TimeOnlyType, TimeNumber(" ([0-9]{2}):([0-9]{2}):([0-9]{2})", 3, "SS")),
                (nameof(ExpressoDbFunctions.AddHours), TimeOnlyType, AddInterval("HOUR")),
                (nameof(ExpressoDbFunctions.AddMinutes), TimeOnlyType, AddInterval("MINUTE")),
                (nameof(ExpressoDbFunctions.AddSeconds), TimeOnlyType, AddInterval("SECOND")),
            };
        }

        private static RelationalTypeMapping DateMapping(SqlExpression value) =>
            value.TypeMapping ?? OracleDate;

        private static RelationalTypeMapping IntervalMapping(SqlExpression value)
        {
            var store = value.TypeMapping?.StoreType;
            return store is not null && store.IndexOf("INTERVAL", StringComparison.OrdinalIgnoreCase) >= 0
                ? value.TypeMapping!
                : OracleInterval;
        }

        /// <summary><c>SUBSTR(s, GREATEST(LENGTH(s) - n + 1, 1))</c>, matching the ADO renderer.</summary>
        private static SqlExpression OracleLiteralRight(SqlExpression value, SqlExpression length)
        {
            var count = Function("LENGTH", typeof(int), OracleNumber, value);
            var start = new SqlBinaryExpression(
                ExpressionType.Add,
                new SqlBinaryExpression(ExpressionType.Subtract, count, length, typeof(int), OracleNumber),
                Fragment("1"),
                typeof(int),
                OracleNumber);
            return Function("SUBSTR", typeof(string), value, Function("GREATEST", typeof(int), OracleNumber, start, Fragment("1")));
        }

        /// <summary>
        /// Clock of a timestamp as <c>INTERVAL DAY TO SECOND</c>, matching <c>value - TRUNC(value)</c> so fractional seconds remain.
        /// </summary>
        private static SqlExpression OracleTimeOfDay(SqlExpression value) =>
            new SqlBinaryExpression(
                ExpressionType.Subtract,
                value,
                Function("TRUNC", typeof(DateTime), OracleDate, value),
                typeof(TimeOnly),
                OracleTimeOnly);

        /// <summary>Oracle EF maps <c>DateOnly</c>/<c>TimeOnly</c> columns to text unless the model sets DATE or INTERVAL.</summary>
        private static void RejectOracleText(SqlExpression value)
        {
            var store = value.TypeMapping?.StoreType;
            if (string.IsNullOrEmpty(store))
            {
                return;
            }

            if (store.IndexOf("CHAR", StringComparison.OrdinalIgnoreCase) >= 0 || store.IndexOf("CLOB", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new NotSupportedException(
                    "Oracle DateOnly/TimeOnly text storage (" + store + ") is not supported. Map DateOnly to DATE and TimeOnly to INTERVAL DAY TO SECOND.");
            }
        }
    }
}
