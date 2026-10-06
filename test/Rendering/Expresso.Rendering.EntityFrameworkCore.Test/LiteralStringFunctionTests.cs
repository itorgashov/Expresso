using System.Linq.Expressions;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.EntityFrameworkCore.Test
{
    /// <summary>Literal string calls stay in SQL, and SQLite <c>right</c> uses a negative <c>substr</c> start.</summary>
    public class LiteralStringFunctionTests
    {
        [Fact]
        public void Literal_ReplaceEmptySearch_StaysInSql()
        {
            // Scenario: an empty search is illegal for string.Replace. Oracle treats it as NULL and returns the source,
            // so both predicates must keep REPLACE and must not become a constant.
            foreach (var context in new[] { TestWidgetContext.SqlServer(), TestWidgetContext.Sqlite(), TestWidgetContext.Oracle() })
            {
                using (context)
                {
                    var equal = Sql(context, new EqFunc(new ReplaceFunc(new Literal("Bob"), new Literal(""), new Literal("x")), new Literal("Bob")));
                    var missing = Sql(context, new IsNullFunc(new ReplaceFunc(new Literal("Bob"), new Literal(""), new Literal("x"))));
                    var deleted = Sql(context, new IsNullFunc(new ReplaceFunc(new Literal("Bob"), new Literal("Bob"), new Literal(""))));
                    Assert.Contains("REPLACE", equal, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("0 = 1", equal);
                    var oracle = context.Database.ProviderName?.Contains("Oracle", StringComparison.Ordinal) == true;
                    if (oracle)
                    {
                        Assert.Contains("REPLACE", missing, StringComparison.OrdinalIgnoreCase);
                        Assert.Contains("REPLACE", deleted, StringComparison.OrdinalIgnoreCase);
                        Assert.DoesNotContain("0 = 1", missing);
                        Assert.DoesNotContain("0 = 1", deleted);
                    }
                    else
                    {
                        Assert.True(IsConstantFalse(missing), missing);
                        Assert.True(IsConstantFalse(deleted), deleted);
                    }
                }
            }
        }

        [Fact]
        public void Sqlite_LiteralStringFunctions_UseStoreFunctions()
        {
            // Scenario: CLR Length/IndexOf/Trim/ToLower/Substring disagree with SQLite on supplementary characters,
            // a tab, and non-ASCII case. The SQL must call the store functions.
            using var context = TestWidgetContext.Sqlite();
            Assert.Contains("length(", Sql(context, new EqFunc(new LenFunc(new Literal("😀")), new Literal(1))), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("instr(", Sql(context, new EqFunc(new IndexOfFunc(new Literal("😀a"), new Literal("a")), new Literal(1))), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("trim(", Sql(context, new EqFunc(new TrimFunc(new Literal("\t")), new Literal("\t"))), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("lower(", Sql(context, new EqFunc(new LowerFunc(new Literal("Ä")), new Literal("Ä"))), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("substr(", Sql(context, new EqFunc(new RightFunc(new Literal("😀a"), new Literal(2)), new Literal("😀a"))), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Sqlite_Right_UsesNegativeSubstr()
        {
            // Scenario: SQLite substr(text, -n) returns the whole string for n <= 0 and for n past the end.
            // The length/substring CASE returns empty for those lengths, so the SQL must be substr with a minus.
            using var context = TestWidgetContext.Sqlite();
            var name = new Field("name", typeof(string));
            foreach (var length in new[] { 0, -1, 2, 10 })
            {
                var sql = Sql(context, new EqFunc(new RightFunc(name, new Literal(length)), name));
                Assert.Contains("substr(", sql, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("-", sql);
                Assert.DoesNotContain("CASE", sql, StringComparison.OrdinalIgnoreCase);
            }

            var notes = Sql(context, new EqFunc(new RightFunc(new Field("notes", typeof(string)), new Literal(0)), new Field("notes", typeof(string))));
            Assert.Contains("substr(", notes, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("-", notes);
            Assert.DoesNotContain("CASE", notes, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void LiteralNegativeRight_UsesAStoreFunction()
        {
            // Scenario: string.Substring throws on a negative length. SQL Server and PostgreSQL already have RIGHT;
            // the other providers must keep a store function for a literal length.
            var filter = new EqFunc(new RightFunc(new Literal("ab"), new Literal(-1)), new Literal("ab"));
            Assert.Equal("Right", Marker(EfCoreProvider.SqlServer, filter));
            Assert.Equal("Right", Marker(EfCoreProvider.PostgreSql, filter));
            Assert.Equal("Right", Marker(EfCoreProvider.Sqlite, filter));
            Assert.Equal("SqlRight", Marker(EfCoreProvider.MySql, filter));
            Assert.Equal("SqlRight", Marker(EfCoreProvider.Oracle, filter));
            Assert.Equal("SqlRight", Marker(EfCoreProvider.Db2, filter));

            using var oracle = TestWidgetContext.Oracle();
            var sql = Sql(oracle, filter);
            Assert.Contains("SUBSTR", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("GREATEST", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Sqlite_LiteralUpperTrimAndLike_UseStoreFunctions()
        {
            // Scenario: CLR upper/trim and case-sensitive StartsWith disagree with SQLite. The command must keep
            // UPPER, LTRIM, RTRIM and LIKE, including when one call is nested inside another.
            using var context = TestWidgetContext.Sqlite();
            var tab = "\t";
            Assert.Contains("UPPER(", Sql(context, new EqFunc(new UpperFunc(new Literal("ä")), new Literal("ä"))), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("LTRIM(", Sql(context, new EqFunc(new LTrimFunc(new Literal(tab)), new Literal(tab))), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("RTRIM(", Sql(context, new EqFunc(new RTrimFunc(new Literal(tab)), new Literal(tab))), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("LIKE", Sql(context, new StrStartswithFunc(new Literal("Ab"), new Literal("a"))), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("LIKE", Sql(context, new StrEndswithFunc(new Literal("bA"), new Literal("a"))), StringComparison.OrdinalIgnoreCase);
            var nested = Sql(context, new EqFunc(new UpperFunc(new LTrimFunc(new Literal("\tä"))), new Literal("ä")));
            Assert.Contains("UPPER(", nested, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("LTRIM(", nested, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Oracle_LiteralTrimSide_KeepsTheCall()
        {
            // Scenario: CLR TrimStart removes a tab and leaves an empty string, which Oracle stores as NULL.
            // Native LTRIM leaves the tab, so isnull must inspect LTRIM rather than a pre-trimmed parameter.
            using var context = TestWidgetContext.Oracle();
            var tab = "\t";
            var missing = Sql(context, new IsNullFunc(new LTrimFunc(new Literal(tab))));
            var trailing = Sql(context, new IsNullFunc(new RTrimFunc(new Literal(tab))));
            Assert.Contains("LTRIM(", missing, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("RTRIM(", trailing, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("0 = 1", missing);
            Assert.DoesNotContain("0 = 1", trailing);
        }

        [Fact]
        public void SqlServer_LiteralLike_StaysInSql()
        {
            // Scenario: a case-insensitive collation matches "Ab" against "a". CLR StartsWith decides false first.
            using var context = TestWidgetContext.SqlServer();
            var start = Sql(context, new StrStartswithFunc(new Literal("Ab"), new Literal("a")));
            var end = Sql(context, new StrEndswithFunc(new Literal("bA"), new Literal("a")));
            var contains = Sql(context, new StrContainsFunc(new Literal("Ab"), new Literal("a")));
            Assert.Contains("LIKE", start, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("LIKE", end, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("LIKE", contains, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("0 = 1", start);
        }

        [Fact]
        public void LiteralStringMarkers_AreNotUsedForFields()
        {
            var field = new EqFunc(new LenFunc(new Field("name", typeof(string))), new Literal(1));
            Assert.Null(Marker(EfCoreProvider.Sqlite, field));
            Assert.Equal("SqlLength", Marker(EfCoreProvider.Sqlite, new EqFunc(new LenFunc(new Literal("a")), new Literal(1))));
            Assert.Equal("SqlReplace", Marker(EfCoreProvider.PostgreSql, new EqFunc(new ReplaceFunc(new Literal("Bob"), new Literal(""), new Literal("x")), new Literal("Bob"))));
            Assert.Equal("IndexOf", Marker(EfCoreProvider.SqlServer, new EqFunc(new IndexOfFunc(new Literal("😀a"), new Literal("a")), new Literal(1))));
            Assert.Equal("SqlIndexOf", Marker(EfCoreProvider.MySql, new EqFunc(new IndexOfFunc(new Literal("😀a"), new Literal("a")), new Literal(1))));
            Assert.Equal("SqlLower", Marker(EfCoreProvider.Db2, new EqFunc(new LowerFunc(new Literal("Ä")), new Literal("Ä"))));
            Assert.Equal("SqlTrim", Marker(EfCoreProvider.Oracle, new EqFunc(new TrimFunc(new Literal("\t")), new Literal("\t"))));
        }

        private static bool IsConstantFalse(string sql) =>
            sql.Contains("0 = 1", StringComparison.Ordinal) || sql.Contains("WHERE 0", StringComparison.Ordinal);

        private static string Sql(TestWidgetContext context, BooleanFunction filter) =>
            context.WhereSql(new FilterCriteria { Expression = filter });

        private static string? Marker(EfCoreProvider provider, BooleanFunction filter)
        {
            var predicate = new EfCoreExpressionToLinqTransformer(provider).BuildPredicate(new FilterCriteria { Expression = filter }, WidgetLinqMapping.Create());
            string? name = null;
            new MarkerVisitor(found => name = found).Visit(predicate);
            return name;
        }

        private sealed class MarkerVisitor : ExpressionVisitor
        {
            private readonly Action<string> _found;

            public MarkerVisitor(Action<string> found) => _found = found;

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.DeclaringType == typeof(ExpressoDbFunctions))
                {
                    _found(node.Method.Name);
                }

                return base.VisitMethodCall(node);
            }
        }
    }
}
