using System.Data.Entity;
using System.Data.SQLite;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Filtering;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.EntityFramework.Test
{
    /// <summary>SQLite EF6 SQL for <c>right</c> (fixed manifest token, no connection).</summary>
    [DbConfigurationType(typeof(TestEf6Configuration))]
    public sealed class SqliteRightContext : DbContext
    {
        public SqliteRightContext()
            : base(new SQLiteConnection("Data Source=:memory:"), contextOwnsConnection: true)
        {
        }

        public DbSet<Widget> Widgets => Set<Widget>();

        public string WhereSql(FilterCriteria filter) =>
            Widgets.Where(new Ef6ExpressionToLinqTransformer(Ef6Provider.Sqlite), filter, WidgetLinqMapping.Create()).Select(w => w.Id).ToString();

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var widget = modelBuilder.Entity<Widget>().ToTable("widget");
            widget.Ignore(w => w.Opens);
            widget.Ignore(w => w.Tags);
        }
    }

    public sealed class SqliteRightTests
    {
        [Fact]
        public void Right_UsesNegativeSubstr()
        {
            // Scenario: substr(text, -n) keeps the whole string when n is zero, negative, or longer than the text.
            // The length/substring CASE returns empty for zero and negative n, so those lengths must not use CASE.
            using var context = new SqliteRightContext();
            var name = new Field("name", typeof(string));
            foreach (var length in new[] { 0, -1, 2, 10 })
            {
                var sql = context.WhereSql(new FilterCriteria { Expression = new EqFunc(new RightFunc(name, new Literal(length)), name) }).ToLowerInvariant();
                Assert.Contains("substr(", sql);
                Assert.Contains("-", sql);
                Assert.DoesNotContain("case", sql);
            }

            var notes = context.WhereSql(new FilterCriteria
            {
                Expression = new EqFunc(new RightFunc(new Field("notes", typeof(string)), new Literal(0)), new Field("notes", typeof(string))),
            }).ToLowerInvariant();
            Assert.Contains("substr(", notes);
            Assert.Contains("-", notes);
            Assert.DoesNotContain("case", notes);
        }

        [Fact]
        public void PowerUnderflow_PreservesRepresentableSubnormals()
        {
            // Scenario: System.Data.SQLite returns 0.5^1075 as a subnormal, so it does not equal 0.
            // Current SQLite returns 0. Only that rounded-to-zero boundary may be corrected; 0.5^1074 stays nonzero.
            using var context = new SqliteRightContext();
            var sql = context.WhereSql(new FilterCriteria
            {
                Expression = new EqFunc(new PowerFunc(new Literal(0.5), new Literal(1075.0)), new Literal(0.0)),
            });

            Assert.Contains("CASE", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ABS", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("POWER", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("LOG(2", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("1075", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("4.94065645841247", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("2.2250738585072", sql, StringComparison.OrdinalIgnoreCase);
        }
    }
}
