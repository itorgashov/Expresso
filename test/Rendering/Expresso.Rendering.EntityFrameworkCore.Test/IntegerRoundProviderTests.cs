using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Rendering.Linq;
using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test
{
    /// <summary>Integer <c>round</c> reaches each provider's SQL, and SQL Server keeps it under <c>isnull</c>.</summary>
    public class IntegerRoundProviderTests
    {
        [Fact]
        public void IntegerLiteral_StaysInProviderRound()
        {
            // Scenario: Math.Round rejects precision 20 and a negative precision. The engine must see ROUND.
            foreach (var context in new[] { Postgres(), Sqlite(), MySql(), Oracle() })
            {
                using (context)
                {
                    var high = Sql(context, new EqFunc(new RoundFunc(new Literal(1), new Literal(20)), new Literal(1.0)));
                    var negative = Sql(context, new EqFunc(new RoundFunc(new Literal(15), new Literal(-1)), new Literal(20.0)));
                    var code = Sql(context, new EqFunc(new RoundFunc(new Literal((byte)1), new Literal(20)), new Literal(1.0)));
                    var floating = Sql(context, new EqFunc(new RoundFunc(new Literal(1.0), new Literal(20)), new Literal(1.0)));
                    AssertRound(high);
                    AssertRound(negative);
                    AssertRound(code);
                    AssertRound(floating);
                    if (context.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true)
                    {
                        Assert.Contains("numeric", high, StringComparison.OrdinalIgnoreCase);
                        Assert.Contains("numeric", floating, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
        }

        [Fact]
        public void PostgreSql_IntegerColumn_UsesNumericRound()
        {
            // Scenario: Math.Round(double, int) is not translated. An int column must use the numeric ROUND marker.
            using var context = Postgres();
            var column = Sql(context, new EqFunc(new RoundFunc(new Field("age", typeof(int)), new Literal(1)), new Literal(1.0)));
            var nullable = Sql(context, new EqFunc(
                new DivFunc(new RoundFunc(new Field("maybe", typeof(int)), new Literal(0)), new Literal(2)),
                new Literal(2.0)));
            AssertRound(column);
            Assert.Contains("numeric", column, StringComparison.OrdinalIgnoreCase);
            AssertRound(nullable);
            Assert.Contains("numeric", nullable, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Maybe", nullable, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SqlServer_IsNullIntegerRound_KeepsTheCall()
        {
            // Scenario: rounding the maximum int at -1 overflows. Dropping ROUND makes isnull false.
            using var context = TestWidgetContext.SqlServer();
            var age = new Field("age", typeof(int));
            var missing = WidgetSql(context, new IsNullFunc(new RoundFunc(age, new Literal(-1))));
            var present = WidgetSql(context, new NotFunc(new IsNullFunc(new RoundFunc(age, new Literal(-1)))));
            var literal = WidgetSql(context, new IsNullFunc(new RoundFunc(new Literal(int.MaxValue), new Literal(-1))));
            var ordinary = WidgetSql(context, new IsNullFunc(new RoundFunc(age, new Literal(0))));
            AssertRound(missing);
            AssertRound(present);
            AssertRound(literal);
            AssertRound(ordinary);
            Assert.DoesNotContain("WHERE 0", missing);
            Assert.DoesNotContain("1 = 0", missing);

            using var ranks = new RoundColumnContext(new DbContextOptionsBuilder<RoundColumnContext>().UseSqlServer("Server=unused;Database=unused").Options);
            var nullable = Sql(ranks, new IsNullFunc(new RoundFunc(new Field("maybe", typeof(int)), new Literal(-1))));
            AssertRound(nullable);
            Assert.Contains("NULL", nullable, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Maybe", nullable, StringComparison.OrdinalIgnoreCase);
        }

        private static void AssertRound(string sql) =>
            Assert.Contains("ROUND", sql, StringComparison.OrdinalIgnoreCase);

        private static string Sql(RoundColumnContext context, BooleanFunction filter) => context.WhereSql(filter);

        private static string WidgetSql(TestWidgetContext context, BooleanFunction filter) =>
            context.WhereSql(new FilterCriteria { Expression = filter });

        private static RoundColumnContext Postgres() =>
            new(new DbContextOptionsBuilder<RoundColumnContext>().UseNpgsql("Host=unused;Database=unused").Options);

        private static RoundColumnContext Sqlite() =>
            new(new DbContextOptionsBuilder<RoundColumnContext>().UseSqlite("Data Source=unused.db").Options);

        private static RoundColumnContext MySql() =>
            new(new DbContextOptionsBuilder<RoundColumnContext>().UseMySql("Server=unused;Database=unused", new MySqlServerVersion(new Version(8, 0, 36))).Options);

        private static RoundColumnContext Oracle() =>
            new(new DbContextOptionsBuilder<RoundColumnContext>().UseOracle("User Id=unused;Password=unused;Data Source=unused").Options);

        private sealed class RoundRow
        {
            public int Id { get; set; }

            public int Age { get; set; }

            public int? Maybe { get; set; }
        }

        private sealed class RoundColumnContext : DbContext
        {
            public RoundColumnContext(DbContextOptions<RoundColumnContext> options)
                : base(options)
            {
            }

            public DbSet<RoundRow> Rows => Set<RoundRow>();

            public string WhereSql(BooleanFunction filter)
            {
                var mapping = new LinqQueryMapping<RoundRow>().Field("age", r => r.Age).Field("maybe", r => r.Maybe);
                var transformer = new EfCoreExpressionToLinqTransformer(Database.ProviderName);
                return Rows.Where(transformer, new FilterCriteria { Expression = filter }, mapping).Select(r => r.Id).ToQueryString();
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.HasExpressoFunctions(Database.ProviderName);
                modelBuilder.Entity<RoundRow>().ToTable("round_row");
            }
        }
    }
}
