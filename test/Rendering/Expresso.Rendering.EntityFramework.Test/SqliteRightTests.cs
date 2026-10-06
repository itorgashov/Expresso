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
    }
}
