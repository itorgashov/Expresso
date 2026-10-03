using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.EntityFramework.Test
{
    /// <summary>Provider resolution and the lambdas built for providers whose EF6 packages the unit tests don't load.</summary>
    public class Ef6ExpressionToLinqTransformerTests
    {
        [Theory]
        [InlineData("System.Data.SqlClient", Ef6Provider.SqlServer)]
        [InlineData("Microsoft.Data.SqlClient", Ef6Provider.SqlServer)]
        [InlineData("Npgsql", Ef6Provider.PostgreSql)]
        [InlineData("MySql.Data.MySqlClient", Ef6Provider.MySql)]
        [InlineData("System.Data.SQLite.EF6", Ef6Provider.Sqlite)]
        [InlineData("System.Data.SQLite", Ef6Provider.Sqlite)]
        [InlineData("Oracle.ManagedDataAccess.Client", Ef6Provider.Oracle)]
        [InlineData("IBM.Data.DB2", Ef6Provider.Other)]
        [InlineData(null, Ef6Provider.Other)]
        public void Resolve_MapsInvariantName(string? invariantName, Ef6Provider expected)
        {
            Assert.Equal(expected, Ef6Providers.Resolve(invariantName));
            Assert.Equal(expected, new Ef6ExpressionToLinqTransformer(invariantName).Provider);
        }

        [Fact]
        public void Context_ResolvesItsConnectionProvider()
        {
            using var context = new TestWidgetEf6Context();
            Assert.Equal(Ef6Provider.SqlServer, context.Transformer().Provider);
        }

        [Fact]
        public void NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => Ef6Providers.Resolve((System.Data.Common.DbConnection)null!));
            Assert.Throws<ArgumentNullException>(() => new Ef6ExpressionToLinqTransformer((System.Data.Entity.DbContext)null!));
        }

        [Theory]
        [InlineData(Ef6Provider.MySql, "adddays", "MySqlAddDate(")]
        [InlineData(Ef6Provider.MySql, "addhours", "MySqlTimestamp(")]
        [InlineData(Ef6Provider.MySql, "addseconds", "MySqlSecToTime(")]
        [InlineData(Ef6Provider.MySql, "time-wrap", "MySqlAddTime(")]
        [InlineData(Ef6Provider.MySql, "date", "MySqlDate(")]
        [InlineData(Ef6Provider.MySql, "time", "MySqlMakeTime(")]
        [InlineData(Ef6Provider.MySql, "dayofweek", "MySqlDayOfWeek(")]
        [InlineData(Ef6Provider.MySql, "dayofyear", "MySqlDayOfYear(")]
        [InlineData(Ef6Provider.MySql, "sqrt", "MySqlSqrt(")]
        [InlineData(Ef6Provider.Sqlite, "dayofweek", "SqliteDatePart(\"weekday\"")]
        [InlineData(Ef6Provider.Sqlite, "dayofyear", "SqliteDatePart(\"dayofyear\"")]
        [InlineData(Ef6Provider.Sqlite, "sqrt", "SqliteSqrt(")]
        [InlineData(Ef6Provider.Sqlite, "indexof-empty", ".Length == 0")]
        [InlineData(Ef6Provider.Sqlite, "contains-case", ".Length == 0) OrElse e.Notes.ToLower().Contains(")]
        [InlineData(Ef6Provider.Sqlite, "startswith-case", ".Length == 0) OrElse e.Name.ToLower().StartsWith(")]
        [InlineData(Ef6Provider.Sqlite, "endswith-case", ".Length == 0) OrElse e.Name.ToLower().EndsWith(")]
        [InlineData(Ef6Provider.PostgreSql, "time", "AddMilliseconds(")]
        [InlineData(Ef6Provider.PostgreSql, "dayofyear", "DiffDays(")]
        [InlineData(Ef6Provider.PostgreSql, "addmonths", "AddMonths(")]
        [InlineData(Ef6Provider.PostgreSql, "concat-null-eq", "?? \"\"")]
        [InlineData(Ef6Provider.Oracle, "dayofyear", "DayOfYear(")]
        [InlineData(Ef6Provider.Oracle, "dayofweek", "DiffDays(")]
        [InlineData(Ef6Provider.Oracle, "date", "TruncateTime(")]
        [InlineData(Ef6Provider.Oracle, "concat-null-all", "(e.Notes == null) AndAlso (e.Notes == null)")]
        [InlineData(Ef6Provider.SqlServer, "indexof-empty", "SqlServerDataLength(")]
        [InlineData(Ef6Provider.Other, "time", "CreateTime(")]
        [InlineData(Ef6Provider.Other, "sqrt", "Sqrt(")]
        [InlineData(Ef6Provider.Other, "left", "Substring(")]
        public void Provider_UsesItsOverride(Ef6Provider provider, string caseId, string call)
        {
            Assert.Contains(call, Predicate(provider, caseId));
        }

        [Theory]
        [InlineData(Ef6Provider.MySql, "concat-null-eq", "?? \"\"")]
        [InlineData(Ef6Provider.Sqlite, "concat-null-eq", "?? \"\"")]
        [InlineData(Ef6Provider.PostgreSql, "left", "Left(")]
        [InlineData(Ef6Provider.PostgreSql, "indexof-empty", "SqlServerDataLength(")]
        [InlineData(Ef6Provider.SqlServer, "contains-case", "ToLower()")]
        public void Provider_KeepsNativeTranslation(Ef6Provider provider, string caseId, string call)
        {
            Assert.DoesNotContain(call, Predicate(provider, caseId));
        }

        [Theory]
        [InlineData(Ef6Provider.PostgreSql, "round", "'round'")]
        [InlineData(Ef6Provider.PostgreSql, "round-digits", "'round'")]
        [InlineData(Ef6Provider.PostgreSql, "sqrt", "'sqrt'")]
        [InlineData(Ef6Provider.Oracle, "sqrt", "'sqrt'")]
        [InlineData(Ef6Provider.Oracle, "adddays", "'adddays'")]
        [InlineData(Ef6Provider.Oracle, "time", "'time'")]
        [InlineData(Ef6Provider.Sqlite, "addhours", "'addhours'")]
        [InlineData(Ef6Provider.Sqlite, "date", "'date'")]
        [InlineData(Ef6Provider.Sqlite, "time", "'time'")]
        [InlineData(Ef6Provider.MySql, "addmonths", "'addmonths'")]
        [InlineData(Ef6Provider.MySql, "addyears", "'addyears'")]
        public void Provider_WithoutExactRendering_Throws(Ef6Provider provider, string caseId, string function)
        {
            var error = Assert.Throws<NotSupportedException>(() => Predicate(provider, caseId));
            Assert.Contains(function, error.Message);
            Assert.Contains(provider.ToString(), error.Message);
        }

        private static string Predicate(Ef6Provider provider, string caseId) =>
            new Ef6ExpressionToLinqTransformer(provider).BuildPredicate(Ef6Cases.Filter(caseId), WidgetLinqMapping.Create()).ToString();
    }
}
