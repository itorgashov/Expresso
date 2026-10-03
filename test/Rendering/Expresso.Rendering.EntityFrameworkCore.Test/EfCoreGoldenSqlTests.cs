using Expresso.Core.Filtering;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.EntityFrameworkCore.Test
{
    /// <summary>Golden SQL (<c>ToQueryString</c>) for every override on the SQL Server and SQLite providers.</summary>
    public class EfCoreGoldenSqlTests
    {
        public static IEnumerable<object[]> SqlServerCases() => new[]
        {
            new object[] { "dayofweek", "WHERE ((DATEPART(weekday, [w].[created_at]) + @@DATEFIRST) - 1) % 7 = @__Value_0" },
            new object[] { "time", "WHERE CAST([w].[created_at] AS time) = @__Value_0" },
            new object[] { "time-wrap", "WHERE DATEPART(hour, DATEADD(hour, @__Value_0, [w].[Opens])) = @__Value_1" },
            new object[] { "time-carry", "WHERE DATEPART(minute, DATEADD(minute, @__Value_0, [w].[Opens])) = @__Value_1" },
            new object[] { "time-seconds", "WHERE DATEPART(second, DATEADD(second, @__Value_0, [w].[Opens])) = @__Value_1" },
            new object[] { "concat-null-eq", "WHERE COALESCE([w].[Name], N'') + COALESCE([w].[Notes], N'') = @__Value_0" },
            new object[] { "avg-fraction", "WHERE [w].[Id] = [w1].[WidgetId]) AS int) AS float) > @__Value_0" },
            new object[] { "indexof-space", "WHEN DATALENGTH(@__Value_0) = 0 THEN 0" },
        };

        public static IEnumerable<object[]> SqliteCases() => new[]
        {
            new object[] { "time", "WHERE time(\"w\".\"created_at\") = @__Value_0" },
            new object[] { "time-hour", "WHERE CAST(strftime('%H', \"w\".\"Opens\") AS INTEGER) = @__Value_0" },
            new object[] { "time-carry", "WHERE CAST(strftime('%M', datetime(\"w\".\"Opens\", printf('%d minutes', @__Value_0))) AS INTEGER) = @__Value_1" },
            new object[] { "time-wrap", "datetime(\"w\".\"Opens\", printf('%d hours', @__Value_0))" },
            new object[] { "time-seconds", "datetime(\"w\".\"Opens\", printf('%d seconds', @__Value_0))" },
            new object[] { "concat-null-eq", "WHERE \"w\".\"Notes\" IS NOT NULL AND \"w\".\"Name\" || COALESCE(\"w\".\"Notes\", '') = @__Value_0" },
        };

        [Theory]
        [MemberData(nameof(SqlServerCases))]
        public void SqlServer_Override_RendersDialectSql(string caseId, string expected)
        {
            using var context = TestWidgetContext.SqlServer();
            Assert.Contains(expected, context.WhereSql(Filter(caseId)));
        }

        [Theory]
        [MemberData(nameof(SqliteCases))]
        public void Sqlite_Override_RendersDialectSql(string caseId, string expected)
        {
            using var context = TestWidgetContext.Sqlite();
            Assert.Contains(expected, context.WhereSql(Filter(caseId)));
        }

        [Fact]
        public void SqlServer_NativeTranslation_KeepsProviderSql()
        {
            // round with no digits is not overridden on SQL Server: EF's own ROUND(x, 0) is used.
            using var context = TestWidgetContext.SqlServer();
            var sql = context.WhereSql(Filter("round"));
            Assert.Contains("ROUND([w].[Amount], 0)", sql);
            Assert.DoesNotContain(nameof(ExpressoDbFunctions), sql);
        }

        [Theory]
        [InlineData("contains", @"'%100\%%'")]
        [InlineData("contains-underscore", @"'%\_off%'")]
        [InlineData("contains-backslash", @"'%\\%'")]
        public void Sqlite_Contains_UsesLikeWithDialectEscaping(string caseId, string pattern)
        {
            using var context = TestWidgetContext.Sqlite();
            var sql = context.WhereSql(Filter(caseId));
            Assert.Matches(@"WHERE ""w"".""Notes"" IS NOT NULL AND ""w"".""Notes"" LIKE @\w+ ESCAPE '\\'", sql);
            Assert.Contains(pattern, sql);
            Assert.DoesNotContain("instr(", sql);
        }

        [Fact]
        public void Sqlite_AverageOfInteger_StaysFractional()
        {
            using var context = TestWidgetContext.Sqlite();
            var sql = context.WhereSql(Filter("avg-fraction"));
            Assert.Contains("AVG(CAST(length(\"w0\".\"Label\") AS REAL))", sql);
            Assert.DoesNotContain("AS INTEGER)", sql);
        }

        private static FilterCriteria Filter(string caseId) =>
            RendererIntegrationCases.AllFilters().SingleOrDefault(c => c.Id == caseId)?.Filter
            ?? RendererDifferentialCases.All().Single(c => c.Id == caseId).Filter!;
    }
}
