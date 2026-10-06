using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Rendering.Linq;
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
        [InlineData(Ef6Provider.SqlServer, "right-long", "Right(")]
        [InlineData(Ef6Provider.PostgreSql, "left", "Substring(")]
        [InlineData(Ef6Provider.PostgreSql, "right-long", "Substring(")]
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

        [Fact]
        public void SqlServer_IsNullSqrt_KeepsSqrt()
        {
            using var context = new TestWidgetEf6Context();
            var filter = new FilterCriteria { Expression = new IsNullFunc(new SqrtFunc(new Field("amount", typeof(double)))) };

            Assert.Contains("SQRT", context.WhereSql(filter), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Oracle_EmptyScalar_IsNull()
        {
            Assert.Contains("True", Predicate(Ef6Provider.Oracle, new IsNullFunc(new ConcatFunc(new List<AbstractExpression> { new Literal(""), new Literal("") }))));
            Assert.Contains("True", Predicate(Ef6Provider.Oracle, new IsNullFunc(new LenFunc(new Literal("")))));
            Assert.Contains("True", Predicate(Ef6Provider.Oracle, new IsNullFunc(new LowerFunc(new Literal("")))));
            Assert.Contains("True", Predicate(Ef6Provider.Oracle, new IsNullFunc(new IndexOfFunc(new Field("name", typeof(string)), new Literal("")))));
        }

        [Fact]
        public void Oracle_MixedEmptyConcat_IsNullOnlyWhenEveryOperandIsNull()
        {
            var name = new Field("name", typeof(string));
            Assert.Contains("False", Predicate(Ef6Provider.Oracle, new IsNullFunc(new ConcatFunc(new List<AbstractExpression> { new Literal("a"), new Literal("") }))));
            Assert.DoesNotContain("False", Predicate(Ef6Provider.Oracle, new EqFunc(new ConcatFunc(new List<AbstractExpression> { new Literal("a"), new Literal("") }), new Literal("a"))));
            Assert.Contains("Name", Predicate(Ef6Provider.Oracle, new IsNullFunc(new ConcatFunc(new List<AbstractExpression> { name, new Literal("") }))));
            Assert.Contains("Name", Predicate(Ef6Provider.Oracle, new EqFunc(new ConcatFunc(new List<AbstractExpression> { name, new Literal("") }), new Literal("Alice"))));
            Assert.Contains("Name", Predicate(Ef6Provider.Oracle, new IsNullFunc(new LenFunc(new ConcatFunc(new List<AbstractExpression> { name, new Literal("") })))));
        }

        [Fact]
        public void Oracle_Replace_NullableSearchOrReplacement_DoesNotNullTheCall()
        {
            // Scenario: a NULL search leaves the source, and a NULL replacement deletes matches.
            // The predicate must not treat a NULL name as a NULL result, including under NOT and LEN.
            var name = new Field("name", typeof(string));
            BooleanFunction[] filters =
            {
                new IsNullFunc(new ReplaceFunc(new Literal("Bob"), name, new Literal("x"))),
                new EqFunc(new ReplaceFunc(new Literal("Bob"), name, new Literal("x")), new Literal("Bob")),
                new EqFunc(new ReplaceFunc(new Literal("Boba"), new Literal("a"), name), new Literal("Bob")),
                new NotFunc(new IsNullFunc(new ReplaceFunc(new Literal("Bob"), name, new Literal("x")))),
                new IsNullFunc(new LenFunc(new ReplaceFunc(new Literal("Bob"), name, new Literal("x")))),
            };

            foreach (var filter in filters)
            {
                var text = Predicate(Ef6Provider.Oracle, filter);
                Assert.Contains("Replace", text);
                Assert.DoesNotContain("Name == null", text);
            }

            Assert.Contains("Name == null", Predicate(Ef6Provider.SqlServer, filters[0]));
        }

        [Fact]
        public void Sqlite_IsNullPower_KeepsPower()
        {
            // Scenario: POWER of a non-nullable double can be NULL. The predicate must not be constant false,
            // including under NOT and for a nullable base. An ordinary exponent stays a real comparison.
            using var context = new SqliteRightContext();
            var amount = new Field("amount", typeof(double));
            var invalid = context.WhereSql(new FilterCriteria { Expression = new IsNullFunc(new PowerFunc(amount, new Literal(0.5))) });
            var ordinary = context.WhereSql(new FilterCriteria { Expression = new EqFunc(new PowerFunc(amount, new Literal(2.0)), new Literal(4.0)) });
            var negated = context.WhereSql(new FilterCriteria { Expression = new NotFunc(new IsNullFunc(new PowerFunc(amount, new Literal(0.5)))) });
            Assert.Contains("power", invalid, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("1 = 0", invalid);
            Assert.Contains("power", ordinary, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("power", negated, StringComparison.OrdinalIgnoreCase);

            var mapping = new LinqQueryMapping<NullablePowerRow>().Field("maybe", r => r.Maybe);
            var text = new Ef6ExpressionToLinqTransformer(Ef6Provider.Sqlite).BuildPredicate(
                new FilterCriteria { Expression = new IsNullFunc(new PowerFunc(new Field("maybe", typeof(double)), new Literal(0.5))) },
                mapping).ToString();
            Assert.Contains("Pow", text);
            Assert.Contains("Maybe", text);
        }

        private sealed class NullablePowerRow
        {
            public double? Maybe { get; set; }
        }

        [Fact]
        public void MySql_DivisionByZero_IsComparedWithZero()
        {
            var text = Predicate(Ef6Provider.MySql, new IsNullFunc(new DivFunc(new Field("amount", typeof(double)), new Literal(0.0))));

            Assert.Contains("== 0", text);
        }

        private static string Predicate(Ef6Provider provider, BooleanFunction expression) =>
            new Ef6ExpressionToLinqTransformer(provider).BuildPredicate(new FilterCriteria { Expression = expression }, WidgetLinqMapping.Create()).ToString();

        private static string Predicate(Ef6Provider provider, string caseId) =>
            new Ef6ExpressionToLinqTransformer(provider).BuildPredicate(Ef6Cases.Filter(caseId), WidgetLinqMapping.Create()).ToString();
    }
}
