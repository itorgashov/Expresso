using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Expresso.Rendering.EntityFrameworkCore.Test
{
    /// <summary>
    /// Every marker translation, printed with placeholder arguments <c>a0</c>, <c>a1</c>. This table is the EF Core
    /// override list per provider; providers without an EF package in this project are covered only here.
    /// </summary>
    public class ExpressoFunctionTranslationsTests
    {
        private static readonly (EfCoreProvider Provider, string Marker, Type Value, string Sql)[] GoldenBase =
        {
            (EfCoreProvider.SqlServer, "DayOfWeek", typeof(DateTime), "((DATEPART(weekday, a0) + @@DATEFIRST) - 1) % 7"),
            (EfCoreProvider.SqlServer, "DayOfWeek", typeof(DateOnly), "((DATEPART(weekday, a0) + @@DATEFIRST) - 1) % 7"),
            (EfCoreProvider.SqlServer, "AddHours", typeof(TimeOnly), "DATEADD(hour, a1, a0)"),
            (EfCoreProvider.SqlServer, "AddMinutes", typeof(TimeOnly), "DATEADD(minute, a1, a0)"),
            (EfCoreProvider.SqlServer, "AddSeconds", typeof(TimeOnly), "DATEADD(second, a1, a0)"),
            (EfCoreProvider.SqlServer, "Time", typeof(DateTime), "CAST(a0 AS time)"),
            (EfCoreProvider.SqlServer, "AddHours", typeof(TimeSpan), "DATEADD(hour, a1, a0)"),
            (EfCoreProvider.SqlServer, "AddMinutes", typeof(TimeSpan), "DATEADD(minute, a1, a0)"),
            (EfCoreProvider.SqlServer, "AddSeconds", typeof(TimeSpan), "DATEADD(second, a1, a0)"),
            (EfCoreProvider.SqlServer, "IndexOf", typeof(string), "CASE WHEN (DATALENGTH(a1) == 0) && a0 IS NOT NULL THEN 0 ELSE CHARINDEX(a1, a0) - 1 END"),
            (EfCoreProvider.SqlServer, "Right", typeof(string), "RIGHT(a0, a1)"),
            (EfCoreProvider.PostgreSql, "Round", typeof(double), "ROUND(CAST(a0 AS numeric), a1)"),
            (EfCoreProvider.PostgreSql, "Left", typeof(string), "LEFT(a0, a1)"),
            (EfCoreProvider.PostgreSql, "Right", typeof(string), "RIGHT(a0, a1)"),
            (EfCoreProvider.MySql, "Time", typeof(DateTime), "CAST(a0 AS time)"),
            (EfCoreProvider.MySql, "AddHours", typeof(TimeSpan), "ADDTIME(a0, SEC_TO_TIME(a1 * 3600))"),
            (EfCoreProvider.MySql, "AddMinutes", typeof(TimeSpan), "ADDTIME(a0, SEC_TO_TIME(a1 * 60))"),
            (EfCoreProvider.MySql, "AddSeconds", typeof(TimeSpan), "ADDTIME(a0, SEC_TO_TIME(a1 * 1))"),
            (EfCoreProvider.MySql, "Hour", typeof(TimeSpan), "HOUR(a0)"),
            (EfCoreProvider.MySql, "Minute", typeof(TimeSpan), "MINUTE(a0)"),
            (EfCoreProvider.MySql, "Second", typeof(TimeSpan), "SECOND(a0)"),
            (EfCoreProvider.MySql, "AddHours", typeof(TimeOnly), "ADDTIME(a0, SEC_TO_TIME(a1 * 3600))"),
            (EfCoreProvider.MySql, "AddMinutes", typeof(TimeOnly), "ADDTIME(a0, SEC_TO_TIME(a1 * 60))"),
            (EfCoreProvider.MySql, "AddSeconds", typeof(TimeOnly), "ADDTIME(a0, SEC_TO_TIME(a1 * 1))"),
            (EfCoreProvider.MySql, "Hour", typeof(TimeOnly), "HOUR(a0)"),
            (EfCoreProvider.MySql, "Minute", typeof(TimeOnly), "MINUTE(a0)"),
            (EfCoreProvider.MySql, "Second", typeof(TimeOnly), "SECOND(a0)"),
            (EfCoreProvider.Sqlite, "Time", typeof(DateTime), "time(a0)"),
            (EfCoreProvider.Sqlite, "AddHours", typeof(TimeSpan), "datetime(a0, printf('%d hours', a1))"),
            (EfCoreProvider.Sqlite, "AddMinutes", typeof(TimeSpan), "datetime(a0, printf('%d minutes', a1))"),
            (EfCoreProvider.Sqlite, "AddSeconds", typeof(TimeSpan), "datetime(a0, printf('%d seconds', a1))"),
            (EfCoreProvider.Sqlite, "Hour", typeof(TimeSpan), "CAST(strftime('%H', a0) AS INTEGER)"),
            (EfCoreProvider.Sqlite, "Minute", typeof(TimeSpan), "CAST(strftime('%M', a0) AS INTEGER)"),
            (EfCoreProvider.Sqlite, "Second", typeof(TimeSpan), "CAST(strftime('%S', a0) AS INTEGER)"),
            (EfCoreProvider.Sqlite, "AddHours", typeof(TimeOnly), "datetime(a0, printf('%d hours', a1))"),
            (EfCoreProvider.Sqlite, "AddMinutes", typeof(TimeOnly), "datetime(a0, printf('%d minutes', a1))"),
            (EfCoreProvider.Sqlite, "AddSeconds", typeof(TimeOnly), "datetime(a0, printf('%d seconds', a1))"),
            (EfCoreProvider.Sqlite, "Hour", typeof(TimeOnly), "CAST(strftime('%H', a0) AS INTEGER)"),
            (EfCoreProvider.Sqlite, "Minute", typeof(TimeOnly), "CAST(strftime('%M', a0) AS INTEGER)"),
            (EfCoreProvider.Sqlite, "Second", typeof(TimeOnly), "CAST(strftime('%S', a0) AS INTEGER)"),
            (EfCoreProvider.Oracle, "DayOfWeek", typeof(DateTime), "TO_NUMBER(TO_CHAR(a0, 'D')) - 1"),
            (EfCoreProvider.Oracle, "DayOfWeek", typeof(DateOnly), "TO_NUMBER(TO_CHAR(a0, 'D')) - 1"),
            (EfCoreProvider.Oracle, "Year", typeof(DateOnly), "TO_NUMBER(TO_CHAR(a0, 'YYYY'))"),
            (EfCoreProvider.Oracle, "Month", typeof(DateOnly), "TO_NUMBER(TO_CHAR(a0, 'MM'))"),
            (EfCoreProvider.Oracle, "Day", typeof(DateOnly), "TO_NUMBER(TO_CHAR(a0, 'DD'))"),
            (EfCoreProvider.Oracle, "DayOfYear", typeof(DateOnly), "TO_NUMBER(TO_CHAR(a0, 'DDD'))"),
            (EfCoreProvider.Oracle, "AddYears", typeof(DateOnly), "a0 + NUMTOYMINTERVAL(a1, 'YEAR')"),
            (EfCoreProvider.Oracle, "AddMonths", typeof(DateOnly), "a0 + NUMTOYMINTERVAL(a1, 'MONTH')"),
            (EfCoreProvider.Oracle, "AddDays", typeof(DateOnly), "a0 + a1"),
            (EfCoreProvider.Oracle, "Hour", typeof(TimeOnly), "TO_NUMBER(REGEXP_SUBSTR(TO_CHAR(a0), ' ([0-9]{2}):', 1, 1, NULL, 1)) * CASE WHEN SUBSTR(TO_CHAR(a0), 1, 1) == '-' THEN -1 ELSE 1 END"),
            (EfCoreProvider.Oracle, "Minute", typeof(TimeOnly), "TO_NUMBER(REGEXP_SUBSTR(TO_CHAR(a0), ' ([0-9]{2}):([0-9]{2}):', 1, 1, NULL, 2)) * CASE WHEN SUBSTR(TO_CHAR(a0), 1, 1) == '-' THEN -1 ELSE 1 END"),
            (EfCoreProvider.Oracle, "Second", typeof(TimeOnly), "TO_NUMBER(REGEXP_SUBSTR(TO_CHAR(a0), ' ([0-9]{2}):([0-9]{2}):([0-9]{2})', 1, 1, NULL, 3)) * CASE WHEN SUBSTR(TO_CHAR(a0), 1, 1) == '-' THEN -1 ELSE 1 END"),
            (EfCoreProvider.Oracle, "AddHours", typeof(TimeOnly), "a0 + NUMTODSINTERVAL(a1, 'HOUR')"),
            (EfCoreProvider.Oracle, "AddMinutes", typeof(TimeOnly), "a0 + NUMTODSINTERVAL(a1, 'MINUTE')"),
            (EfCoreProvider.Oracle, "AddSeconds", typeof(TimeOnly), "a0 + NUMTODSINTERVAL(a1, 'SECOND')"),
            (EfCoreProvider.Oracle, "Date", typeof(DateTime), "TRUNC(a0)"),
            (EfCoreProvider.Oracle, "Time", typeof(DateTime), "a0 - TRUNC(a0)"),
            (EfCoreProvider.Oracle, "IndexOf", typeof(string), "INSTR(a0, a1) - 1"),
            (EfCoreProvider.Oracle, "IsNullValue", typeof(string), "CASE WHEN NULLIF(a0, NULL) == NULL THEN 1 ELSE 0 END == 1"),
            (EfCoreProvider.Db2, "DayOfWeek", typeof(DateTime), "DAYOFWEEK(a0) - 1"),
            (EfCoreProvider.Db2, "DayOfWeek", typeof(DateOnly), "DAYOFWEEK(a0) - 1"),
            (EfCoreProvider.Db2, "AddYears", typeof(DateOnly), "ADD_YEARS(a0, a1)"),
            (EfCoreProvider.Db2, "AddMonths", typeof(DateOnly), "ADD_DAYS(ADD_MONTHS(a0, a1), LEAST(0, DAY(a0) - DAY(ADD_MONTHS(a0, a1))))"),
            (EfCoreProvider.Db2, "AddDays", typeof(DateOnly), "ADD_DAYS(a0, a1)"),
            (EfCoreProvider.Db2, "AddHours", typeof(TimeOnly), "TIME(ADD_HOURS(TIMESTAMP('2000-01-01', a0), a1))"),
            (EfCoreProvider.Db2, "AddMinutes", typeof(TimeOnly), "TIME(ADD_MINUTES(TIMESTAMP('2000-01-01', a0), a1))"),
            (EfCoreProvider.Db2, "AddSeconds", typeof(TimeOnly), "TIME(ADD_SECONDS(TIMESTAMP('2000-01-01', a0), a1))"),
            (EfCoreProvider.Db2, "Hour", typeof(TimeOnly), "HOUR(a0)"),
            (EfCoreProvider.Db2, "Minute", typeof(TimeOnly), "MINUTE(a0)"),
            (EfCoreProvider.Db2, "Second", typeof(TimeOnly), "SECOND(a0)"),
            (EfCoreProvider.Db2, "Date", typeof(DateTime), "DATE(a0)"),
            (EfCoreProvider.Db2, "Time", typeof(DateTime), "TIME(a0)"),
            (EfCoreProvider.Db2, "Round", typeof(double), "ROUND(a0, a1)"),
            (EfCoreProvider.Db2, "IndexOf", typeof(string), "LOCATE(a1, a0) - 1"),
            (EfCoreProvider.Db2, "AddYears", typeof(DateTime), "ADD_YEARS(a0, a1)"),
            (EfCoreProvider.Db2, "AddMonths", typeof(DateTime), "ADD_DAYS(ADD_MONTHS(a0, a1), LEAST(0, DAY(a0) - DAY(ADD_MONTHS(a0, a1))))"),
            (EfCoreProvider.Db2, "AddDays", typeof(DateTime), "ADD_DAYS(a0, a1)"),
            (EfCoreProvider.Db2, "AddHours", typeof(DateTime), "ADD_HOURS(a0, a1)"),
            (EfCoreProvider.Db2, "AddMinutes", typeof(DateTime), "ADD_MINUTES(a0, a1)"),
            (EfCoreProvider.Db2, "AddSeconds", typeof(DateTime), "ADD_SECONDS(a0, a1)"),
            (EfCoreProvider.Db2, "AddHours", typeof(TimeSpan), "TIME(ADD_HOURS(TIMESTAMP('2000-01-01', a0), a1))"),
            (EfCoreProvider.Db2, "AddMinutes", typeof(TimeSpan), "TIME(ADD_MINUTES(TIMESTAMP('2000-01-01', a0), a1))"),
            (EfCoreProvider.Db2, "AddSeconds", typeof(TimeSpan), "TIME(ADD_SECONDS(TIMESTAMP('2000-01-01', a0), a1))"),
            (EfCoreProvider.Db2, "Hour", typeof(TimeSpan), "HOUR(a0)"),
            (EfCoreProvider.Db2, "Minute", typeof(TimeSpan), "MINUTE(a0)"),
            (EfCoreProvider.Db2, "Second", typeof(TimeSpan), "SECOND(a0)"),
            (EfCoreProvider.SqlServer, "IsDomainNull", typeof(double), "CASE WHEN NULLIF(a0, NULL) == NULL THEN 1 ELSE 0 END == 1"),
            (EfCoreProvider.SqlServer, "IsDomainNull", typeof(int), "CASE WHEN NULLIF(a0, NULL) == NULL THEN 1 ELSE 0 END == 1"),
            (EfCoreProvider.PostgreSql, "IsDomainNull", typeof(double), "CASE WHEN NULLIF(a0, NULL) == NULL THEN 1 ELSE 0 END == 1"),
            (EfCoreProvider.PostgreSql, "IsDomainNull", typeof(int), "CASE WHEN NULLIF(a0, NULL) == NULL THEN 1 ELSE 0 END == 1"),
            (EfCoreProvider.Oracle, "IsDomainNull", typeof(double), "CASE WHEN NULLIF(a0, NULL) == NULL THEN 1 ELSE 0 END == 1"),
            (EfCoreProvider.Oracle, "IsDomainNull", typeof(int), "CASE WHEN NULLIF(a0, NULL) == NULL THEN 1 ELSE 0 END == 1"),
            (EfCoreProvider.Db2, "IsDomainNull", typeof(double), "CASE WHEN NULLIF(a0, NULL) == NULL THEN 1 ELSE 0 END == 1"),
            (EfCoreProvider.Db2, "IsDomainNull", typeof(int), "CASE WHEN NULLIF(a0, NULL) == NULL THEN 1 ELSE 0 END == 1"),
            (EfCoreProvider.MySql, "IsDomainNull", typeof(double), "CASE WHEN NULLIF(a0, NULL) == NULL THEN 1 ELSE 0 END == 1"),
            (EfCoreProvider.MySql, "IsDomainNull", typeof(int), "CASE WHEN NULLIF(a0, NULL) == NULL THEN 1 ELSE 0 END == 1"),
            (EfCoreProvider.Sqlite, "IsDomainNull", typeof(double), "CASE WHEN NULLIF(a0, NULL) == NULL THEN 1 ELSE 0 END == 1"),
            (EfCoreProvider.Sqlite, "IsDomainNull", typeof(int), "CASE WHEN NULLIF(a0, NULL) == NULL THEN 1 ELSE 0 END == 1"),
            (EfCoreProvider.SqlServer, "Sqrt", typeof(double), "SQRT(a0)"),
            (EfCoreProvider.SqlServer, "Divide", typeof(double), "a0 / a1"),
            (EfCoreProvider.SqlServer, "Divide", typeof(int), "a0 / a1"),
            (EfCoreProvider.SqlServer, "Modulo", typeof(double), "a0 % a1"),
            (EfCoreProvider.SqlServer, "Modulo", typeof(int), "a0 % a1"),
            (EfCoreProvider.SqlServer, "SqlSubstring", typeof(string), "SUBSTRING(a0, a1, a2)"),
            (EfCoreProvider.PostgreSql, "Sqrt", typeof(double), "SQRT(a0)"),
            (EfCoreProvider.PostgreSql, "Divide", typeof(double), "a0 / a1"),
            (EfCoreProvider.PostgreSql, "Divide", typeof(int), "a0 / a1"),
            (EfCoreProvider.PostgreSql, "Modulo", typeof(double), "a0 % a1"),
            (EfCoreProvider.PostgreSql, "Modulo", typeof(int), "a0 % a1"),
            (EfCoreProvider.PostgreSql, "SqlSubstring", typeof(string), "substr(a0, a1, a2)"),
            (EfCoreProvider.MySql, "Sqrt", typeof(double), "SQRT(a0)"),
            (EfCoreProvider.MySql, "Divide", typeof(double), "a0 / a1"),
            (EfCoreProvider.MySql, "Divide", typeof(int), "a0 / a1"),
            (EfCoreProvider.MySql, "Modulo", typeof(double), "a0 % a1"),
            (EfCoreProvider.MySql, "Modulo", typeof(int), "a0 % a1"),
            (EfCoreProvider.MySql, "SqlSubstring", typeof(string), "SUBSTRING(a0, a1, a2)"),
            (EfCoreProvider.Sqlite, "Divide", typeof(double), "a0 / a1"),
            (EfCoreProvider.Sqlite, "Divide", typeof(int), "a0 / a1"),
            (EfCoreProvider.Sqlite, "Modulo", typeof(double), "a0 % a1"),
            (EfCoreProvider.Sqlite, "Modulo", typeof(int), "a0 % a1"),
            (EfCoreProvider.Sqlite, "SqlSubstring", typeof(string), "substr(a0, a1, a2)"),
            (EfCoreProvider.Oracle, "Sqrt", typeof(double), "SQRT(a0)"),
            (EfCoreProvider.Oracle, "Divide", typeof(double), "a0 / a1"),
            (EfCoreProvider.Oracle, "Divide", typeof(int), "a0 / a1"),
            (EfCoreProvider.Oracle, "Modulo", typeof(double), "a0 % a1"),
            (EfCoreProvider.Oracle, "Modulo", typeof(int), "a0 % a1"),
            (EfCoreProvider.Oracle, "SqlSubstring", typeof(string), "SUBSTR(a0, a1, a2)"),
            (EfCoreProvider.Db2, "Sqrt", typeof(double), "SQRT(a0)"),
            (EfCoreProvider.Db2, "Divide", typeof(double), "a0 / a1"),
            (EfCoreProvider.Db2, "Divide", typeof(int), "a0 / a1"),
            (EfCoreProvider.Db2, "Modulo", typeof(double), "a0 % a1"),
            (EfCoreProvider.Db2, "Modulo", typeof(int), "a0 % a1"),
            (EfCoreProvider.Db2, "SqlSubstring", typeof(string), "SUBSTR(a0, a1, a2)"),
            (EfCoreProvider.SqlServer, "SqlReplace", typeof(string), "REPLACE(a0, a1, a2)"),
            (EfCoreProvider.SqlServer, "SqlLength", typeof(string), "LEN(a0)"),
            (EfCoreProvider.SqlServer, "SqlLower", typeof(string), "LOWER(a0)"),
            (EfCoreProvider.SqlServer, "SqlTrim", typeof(string), "TRIM(a0)"),
            (EfCoreProvider.PostgreSql, "SqlReplace", typeof(string), "REPLACE(a0, a1, a2)"),
            (EfCoreProvider.PostgreSql, "SqlLength", typeof(string), "LENGTH(a0)"),
            (EfCoreProvider.PostgreSql, "SqlLower", typeof(string), "LOWER(a0)"),
            (EfCoreProvider.PostgreSql, "SqlTrim", typeof(string), "TRIM(a0)"),
            (EfCoreProvider.PostgreSql, "SqlIndexOf", typeof(string), "STRPOS(a0, a1) - 1"),
            (EfCoreProvider.MySql, "SqlReplace", typeof(string), "REPLACE(a0, a1, a2)"),
            (EfCoreProvider.MySql, "SqlLength", typeof(string), "CHAR_LENGTH(a0)"),
            (EfCoreProvider.MySql, "SqlLower", typeof(string), "LOWER(a0)"),
            (EfCoreProvider.MySql, "SqlTrim", typeof(string), "TRIM(a0)"),
            (EfCoreProvider.MySql, "SqlIndexOf", typeof(string), "LOCATE(a1, a0) - 1"),
            (EfCoreProvider.MySql, "SqlRight", typeof(string), "RIGHT(a0, a1)"),
            (EfCoreProvider.Sqlite, "Right", typeof(string), "substr(a0, Negate(a1))"),
            (EfCoreProvider.Sqlite, "SqlReplace", typeof(string), "REPLACE(a0, a1, a2)"),
            (EfCoreProvider.Sqlite, "SqlLength", typeof(string), "length(a0)"),
            (EfCoreProvider.Sqlite, "SqlLower", typeof(string), "LOWER(a0)"),
            (EfCoreProvider.Sqlite, "SqlTrim", typeof(string), "TRIM(a0)"),
            (EfCoreProvider.Sqlite, "SqlIndexOf", typeof(string), "instr(a0, a1) - 1"),
            (EfCoreProvider.Oracle, "SqlReplace", typeof(string), "REPLACE(a0, a1, a2)"),
            (EfCoreProvider.Oracle, "SqlLength", typeof(string), "LENGTH(a0)"),
            (EfCoreProvider.Oracle, "SqlLower", typeof(string), "LOWER(a0)"),
            (EfCoreProvider.Oracle, "SqlTrim", typeof(string), "TRIM(a0)"),
            (EfCoreProvider.Oracle, "SqlRight", typeof(string), "SUBSTR(a0, GREATEST((LENGTH(a0) - a1) + 1, 1))"),
            (EfCoreProvider.Db2, "SqlReplace", typeof(string), "REPLACE(a0, a1, a2)"),
            (EfCoreProvider.Db2, "SqlLength", typeof(string), "LENGTH(a0)"),
            (EfCoreProvider.Db2, "SqlLower", typeof(string), "LOWER(a0)"),
            (EfCoreProvider.Db2, "SqlTrim", typeof(string), "TRIM(a0)"),
            (EfCoreProvider.Db2, "SqlRight", typeof(string), "RIGHT(a0, a1)"),
            (EfCoreProvider.SqlServer, "SqlUpper", typeof(string), "UPPER(a0)"),
            (EfCoreProvider.SqlServer, "SqlLTrim", typeof(string), "LTRIM(a0)"),
            (EfCoreProvider.SqlServer, "SqlRTrim", typeof(string), "RTRIM(a0)"),
            (EfCoreProvider.SqlServer, "SqlPower", typeof(double), "POWER(a0, a1)"),
            (EfCoreProvider.SqlServer, "SqlAbs", typeof(double), "ABS(a0)"),
            (EfCoreProvider.SqlServer, "SqlAbs", typeof(int), "ABS(a0)"),
            (EfCoreProvider.SqlServer, "SqlRound", typeof(double), "ROUND(a0, a1)"),
            (EfCoreProvider.PostgreSql, "SqlUpper", typeof(string), "UPPER(a0)"),
            (EfCoreProvider.PostgreSql, "SqlLTrim", typeof(string), "LTRIM(a0)"),
            (EfCoreProvider.PostgreSql, "SqlRTrim", typeof(string), "RTRIM(a0)"),
            (EfCoreProvider.PostgreSql, "SqlPower", typeof(double), "POWER(a0, a1)"),
            (EfCoreProvider.PostgreSql, "SqlAbs", typeof(double), "ABS(a0)"),
            (EfCoreProvider.PostgreSql, "SqlAbs", typeof(int), "ABS(a0)"),
            (EfCoreProvider.MySql, "SqlUpper", typeof(string), "UPPER(a0)"),
            (EfCoreProvider.MySql, "SqlLTrim", typeof(string), "LTRIM(a0)"),
            (EfCoreProvider.MySql, "SqlRTrim", typeof(string), "RTRIM(a0)"),
            (EfCoreProvider.MySql, "SqlPower", typeof(double), "POWER(a0, a1)"),
            (EfCoreProvider.MySql, "SqlAbs", typeof(double), "ABS(a0)"),
            (EfCoreProvider.MySql, "SqlAbs", typeof(int), "ABS(a0)"),
            (EfCoreProvider.MySql, "SqlRound", typeof(double), "ROUND(a0, a1)"),
            (EfCoreProvider.Sqlite, "SqlUpper", typeof(string), "UPPER(a0)"),
            (EfCoreProvider.Sqlite, "SqlLTrim", typeof(string), "LTRIM(a0)"),
            (EfCoreProvider.Sqlite, "SqlRTrim", typeof(string), "RTRIM(a0)"),
            (EfCoreProvider.Sqlite, "SqlPower", typeof(double), "POWER(a0, a1)"),
            (EfCoreProvider.Sqlite, "SqlAbs", typeof(double), "ABS(a0)"),
            (EfCoreProvider.Sqlite, "SqlAbs", typeof(int), "ABS(a0)"),
            (EfCoreProvider.Sqlite, "SqlRound", typeof(double), "ROUND(a0, a1)"),
            (EfCoreProvider.Oracle, "SqlUpper", typeof(string), "UPPER(a0)"),
            (EfCoreProvider.Oracle, "SqlLTrim", typeof(string), "LTRIM(a0)"),
            (EfCoreProvider.Oracle, "SqlRTrim", typeof(string), "RTRIM(a0)"),
            (EfCoreProvider.Oracle, "SqlPower", typeof(double), "POWER(a0, a1)"),
            (EfCoreProvider.Oracle, "SqlAbs", typeof(double), "ABS(a0)"),
            (EfCoreProvider.Oracle, "SqlAbs", typeof(int), "ABS(a0)"),
            (EfCoreProvider.Oracle, "SqlRound", typeof(double), "ROUND(a0, a1)"),
            (EfCoreProvider.Db2, "SqlUpper", typeof(string), "UPPER(a0)"),
            (EfCoreProvider.Db2, "SqlLTrim", typeof(string), "LTRIM(a0)"),
            (EfCoreProvider.Db2, "SqlRTrim", typeof(string), "RTRIM(a0)"),
            (EfCoreProvider.Db2, "SqlPower", typeof(double), "POWER(a0, a1)"),
            (EfCoreProvider.Db2, "SqlAbs", typeof(double), "ABS(a0)"),
            (EfCoreProvider.Db2, "SqlAbs", typeof(int), "ABS(a0)"),
        };

        public static IEnumerable<object[]> GoldenCases() =>
            GoldenBase.Select(g => new object[] { g.Provider, g.Marker, g.Value, g.Sql });

        public static IEnumerable<object[]> Markers() =>
            typeof(ExpressoDbFunctions).GetMethods(BindingFlags.Public | BindingFlags.Static).Select(m => new object[] { m.Name, m.GetParameters()[0].ParameterType });

        [Theory]
        [MemberData(nameof(GoldenCases))]
        public void Translation_RendersDialectSql(EfCoreProvider provider, string marker, Type value, string expected)
        {
            var (method, translation) = ExpressoFunctionTranslations.For(provider)
                .Single(e => e.Key.Name == marker && e.Key.GetParameters()[0].ParameterType == value);
            var arguments = method.GetParameters().Select((_, i) => (SqlExpression)new SqlFragmentExpression("a" + i)).ToList();

            var printed = new ExpressionPrinter().PrintExpression(translation(arguments));

            Assert.Equal(expected, Regex.Replace(printed, @"\s+", " "));
        }

        [Fact]
        public void For_EveryRegisteredTranslation_HasGoldenSql()
        {
            foreach (var provider in Enum.GetValues<EfCoreProvider>())
            {
                foreach (var (method, translation) in ExpressoFunctionTranslations.For(provider))
                {
                    var arguments = method.GetParameters().Select((_, i) => (SqlExpression)new SqlFragmentExpression("a" + i)).ToList();
                    var printed = new ExpressionPrinter().PrintExpression(translation(arguments));
                    Assert.False(string.IsNullOrWhiteSpace(printed));
                }
            }

            var goldenKeys = GoldenBase.Select(g => (g.Provider, g.Marker, g.Value)).ToHashSet();
            var missing = new List<string>();
            foreach (var provider in Enum.GetValues<EfCoreProvider>())
            {
                foreach (var (method, translation) in ExpressoFunctionTranslations.For(provider))
                {
                    var value = method.GetParameters()[0].ParameterType;
                    if (!goldenKeys.Contains((provider, method.Name, value)))
                    {
                        var arguments = method.GetParameters().Select((_, i) => (SqlExpression)new SqlFragmentExpression("a" + i)).ToList();
                        var printed = Regex.Replace(new ExpressionPrinter().PrintExpression(translation(arguments)), @"\s+", " ");
                        missing.Add(provider + " " + method.Name + " " + value.Name + " :: " + printed);
                    }
                }
            }

            Assert.True(missing.Count == 0, "Registered translations without golden SQL:" + Environment.NewLine + string.Join(Environment.NewLine, missing));
        }

        [Fact]
        public void For_OtherProvider_HasNoTranslations()
        {
            Assert.Empty(ExpressoFunctionTranslations.For(EfCoreProvider.Other));
            Assert.Null(ExpressoFunctionTranslations.Find(EfCoreProvider.Other, "Time", new[] { typeof(DateTime) }));
        }

        [Fact]
        public void Find_MatchesNameAndArgumentTypes()
        {
            var timeOfDay = ExpressoFunctionTranslations.Find(EfCoreProvider.Db2, "AddHours", new[] { typeof(TimeSpan), typeof(int) });
            var timestamp = ExpressoFunctionTranslations.Find(EfCoreProvider.Db2, "AddHours", new[] { typeof(DateTime), typeof(int) });

            Assert.Equal(typeof(TimeSpan), timeOfDay!.ReturnType);
            Assert.Equal(typeof(DateTime), timestamp!.ReturnType);
            Assert.Null(ExpressoFunctionTranslations.Find(EfCoreProvider.Db2, "AddHours", new[] { typeof(DateTime), typeof(double) }));
            Assert.Null(ExpressoFunctionTranslations.Find(EfCoreProvider.SqlServer, "Round", new[] { typeof(double), typeof(int) }));
        }

        [Theory]
        [MemberData(nameof(Markers))]
        public void Marker_CalledInMemory_Throws(string name, Type value)
        {
            var method = typeof(ExpressoDbFunctions).GetMethods().Single(m => m.Name == name && m.GetParameters()[0].ParameterType == value);
            var arguments = method.GetParameters().Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : "").ToArray();

            var ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, arguments));

            var inner = Assert.IsType<InvalidOperationException>(ex.InnerException);
            Assert.Contains($"ExpressoDbFunctions.{name}", inner.Message);
        }
    }
}
