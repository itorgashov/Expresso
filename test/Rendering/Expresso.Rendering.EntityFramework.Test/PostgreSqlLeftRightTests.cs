using System.Data.Entity;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Filtering;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Npgsql;

namespace Expresso.Rendering.EntityFramework.Test
{
    /// <summary>Npgsql EF6 SQL for negative <c>left</c>/<c>right</c> (fixed manifest token, no connection).</summary>
    [DbConfigurationType(typeof(TestEf6Configuration))]
    public sealed class PostgreSqlLeftRightContext : DbContext
    {
        public PostgreSqlLeftRightContext()
            : base(new NpgsqlConnection("Host=unused;Database=unused"), contextOwnsConnection: true)
        {
        }

        public DbSet<Widget> Widgets => Set<Widget>();

        public string WhereSql(FilterCriteria filter) =>
            Widgets.Where(new Ef6ExpressionToLinqTransformer(Ef6Provider.PostgreSql), filter, WidgetLinqMapping.Create()).Select(w => w.Id).ToString();

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Widget>().ToTable("widget");
        }
    }

    public sealed class PostgreSqlLeftRightTests
    {
        [Fact]
        public void NegativeLeft_UsesCaseNotNegativeSubstrLength()
        {
            using var context = new PostgreSqlLeftRightContext();
            var filter = new FilterCriteria
            {
                Expression = new EqFunc(new LeftFunc(new Field("name", typeof(string)), new Literal(-1)), new Literal("ab")),
            };

            var sql = context.WhereSql(filter).ToLowerInvariant();

            Assert.Contains("case", sql);
            Assert.Contains("substr", sql);
            Assert.DoesNotContain("left(", sql);
        }

        [Fact]
        public void NegativeRight_UsesCase()
        {
            using var context = new PostgreSqlLeftRightContext();
            var filter = new FilterCriteria
            {
                Expression = new EqFunc(new RightFunc(new Field("name", typeof(string)), new Literal(-1)), new Literal("bc")),
            };

            var sql = context.WhereSql(filter).ToLowerInvariant();

            Assert.Contains("case", sql);
            Assert.Contains("substr", sql);
            Assert.DoesNotContain("right(", sql);
        }
    }
}
