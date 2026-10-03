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
        private static readonly (EfCoreProvider Provider, string Marker, Type Value, string Sql)[] Golden =
        {
            (EfCoreProvider.SqlServer, "DayOfWeek", typeof(DateTime), "((DATEPART(weekday, a0) + @@DATEFIRST) - 1) % 7"),
            (EfCoreProvider.SqlServer, "Time", typeof(DateTime), "CAST(a0 AS time)"),
            (EfCoreProvider.SqlServer, "AddHours", typeof(TimeSpan), "DATEADD(hour, a1, a0)"),
            (EfCoreProvider.SqlServer, "AddMinutes", typeof(TimeSpan), "DATEADD(minute, a1, a0)"),
            (EfCoreProvider.SqlServer, "AddSeconds", typeof(TimeSpan), "DATEADD(second, a1, a0)"),
            (EfCoreProvider.SqlServer, "IndexOf", typeof(string), "CASE WHEN (DATALENGTH(a1) == 0) && a0 IS NOT NULL THEN 0 ELSE CHARINDEX(a1, a0) - 1 END"),
            (EfCoreProvider.PostgreSql, "Round", typeof(double), "ROUND(CAST(a0 AS numeric), a1)"),
            (EfCoreProvider.MySql, "Time", typeof(DateTime), "CAST(a0 AS time)"),
            (EfCoreProvider.MySql, "AddHours", typeof(TimeSpan), "ADDTIME(a0, SEC_TO_TIME(a1 * 3600))"),
            (EfCoreProvider.MySql, "AddMinutes", typeof(TimeSpan), "ADDTIME(a0, SEC_TO_TIME(a1 * 60))"),
            (EfCoreProvider.MySql, "AddSeconds", typeof(TimeSpan), "ADDTIME(a0, SEC_TO_TIME(a1 * 1))"),
            (EfCoreProvider.MySql, "Hour", typeof(TimeSpan), "HOUR(a0)"),
            (EfCoreProvider.MySql, "Minute", typeof(TimeSpan), "MINUTE(a0)"),
            (EfCoreProvider.MySql, "Second", typeof(TimeSpan), "SECOND(a0)"),
            (EfCoreProvider.Sqlite, "Time", typeof(DateTime), "time(a0)"),
            (EfCoreProvider.Sqlite, "AddHours", typeof(TimeSpan), "datetime(a0, printf('%d hours', a1))"),
            (EfCoreProvider.Sqlite, "AddMinutes", typeof(TimeSpan), "datetime(a0, printf('%d minutes', a1))"),
            (EfCoreProvider.Sqlite, "AddSeconds", typeof(TimeSpan), "datetime(a0, printf('%d seconds', a1))"),
            (EfCoreProvider.Sqlite, "Hour", typeof(TimeSpan), "CAST(strftime('%H', a0) AS INTEGER)"),
            (EfCoreProvider.Sqlite, "Minute", typeof(TimeSpan), "CAST(strftime('%M', a0) AS INTEGER)"),
            (EfCoreProvider.Sqlite, "Second", typeof(TimeSpan), "CAST(strftime('%S', a0) AS INTEGER)"),
            (EfCoreProvider.Oracle, "DayOfWeek", typeof(DateTime), "TO_NUMBER(TO_CHAR(a0, 'D')) - 1"),
            (EfCoreProvider.Oracle, "Date", typeof(DateTime), "TO_CHAR(TRUNC(a0), 'YYYY-MM-DD')"),
            (EfCoreProvider.Oracle, "Time", typeof(DateTime),
                "CASE WHEN TO_CHAR(a0, 'FF7') == '0000000' THEN TO_CHAR(a0, 'HH24:MI:SS') ELSE TO_CHAR(a0, 'HH24:MI:SS.FF7') END"),
            (EfCoreProvider.Oracle, "IndexOf", typeof(string), "INSTR(a0, a1) - 1"),
            (EfCoreProvider.Db2, "DayOfWeek", typeof(DateTime), "DAYOFWEEK(a0) - 1"),
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
        };

        public static IEnumerable<object[]> GoldenCases() =>
            Golden.Select(g => new object[] { g.Provider, g.Marker, g.Value, g.Sql });

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
            var registered = Enum.GetValues<EfCoreProvider>()
                .SelectMany(p => ExpressoFunctionTranslations.For(p).Select(e => (p, e.Key.Name, e.Key.GetParameters()[0].ParameterType)))
                .OrderBy(e => e.ToString());
            Assert.Equal(Golden.Select(g => (g.Provider, g.Marker, g.Value)).OrderBy(e => e.ToString()), registered);
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
