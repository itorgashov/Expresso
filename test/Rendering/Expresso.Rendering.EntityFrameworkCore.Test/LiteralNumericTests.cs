using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test
{
    /// <summary>Literal round/power/abs stay in SQL, and <c>isnull(power)</c> keeps the call.</summary>
    public class LiteralNumericTests
    {
        [Fact]
        public void LiteralRoundPowerAndAbs_StayInSql()
        {
            // Scenario: Math.Round uses midpoint-to-even and rejects precision 20, Math.Pow turns a domain result into
            // NaN, and Math.Abs(int.MinValue) overflows. The engine must see the call.
            foreach (var context in new[] { TestWidgetContext.SqlServer(), TestWidgetContext.Sqlite(), TestWidgetContext.Oracle() })
            {
                using (context)
                {
                    var midpoint = Sql(context, new EqFunc(new RoundFunc(new Literal(2.5)), new Literal(3.0)));
                    var precise = Sql(context, new EqFunc(new RoundFunc(new Literal(2.54), new Literal(20)), new Literal(2.54)));
                    var power = Sql(context, new NotFunc(new EqFunc(new PowerFunc(new Literal(-1.0), new Literal(0.5)), new Literal(0.0))));
                    var absolute = Sql(context, new EqFunc(new AbsFunc(new Literal(int.MinValue)), new Literal(0)));
                    Assert.Contains("ROUND", midpoint, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("0 = 1", midpoint);
                    Assert.Contains("ROUND", precise, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("POWER", power, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("0 = 1", power);
                    Assert.Contains("ABS", absolute, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        [Fact]
        public void IsNullPower_KeepsTheCall()
        {
            // Scenario: power of a non-nullable double can still be NULL. Folding isnull to false drops that row.
            // A nullable base keeps both its own null check and POWER. An ordinary exponent is not folded away either.
            using var context = new NumberContext();
            var number = new Field("number", typeof(double));
            var maybe = new Field("maybe", typeof(double));
            var invalid = Sql(context, new IsNullFunc(new PowerFunc(number, new Literal(0.5))));
            var ordinary = Sql(context, new IsNullFunc(new PowerFunc(number, new Literal(2.0))));
            var negated = Sql(context, new NotFunc(new IsNullFunc(new PowerFunc(number, new Literal(0.5)))));
            var compared = Sql(context, new EqFunc(new PowerFunc(number, new Literal(2.0)), new Literal(4.0)));
            var nullable = Sql(context, new IsNullFunc(new PowerFunc(maybe, new Literal(0.5))));
            Assert.Contains("POWER", invalid, StringComparison.Ordinal);
            Assert.Contains("NULLIF", invalid, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("WHERE 0", invalid);
            Assert.Contains("POWER", ordinary, StringComparison.Ordinal);
            Assert.Contains("POWER", negated, StringComparison.Ordinal);
            Assert.Contains("POWER", compared, StringComparison.Ordinal);
            Assert.Contains("POWER", nullable, StringComparison.Ordinal);
            Assert.Contains("Maybe", nullable, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SqlServerAndOracle_IsNullPower_KeepsPower()
        {
            // Scenario: the same non-nullable power call is deleted on every provider when its null state is dropped.
            foreach (var context in new[] { TestWidgetContext.SqlServer(), TestWidgetContext.Oracle() })
            {
                using (context)
                {
                    var sql = context.WhereSql(new FilterCriteria
                    {
                        Expression = new IsNullFunc(new PowerFunc(new Field("amount", typeof(double)), new Literal(0.5))),
                    });
                    Assert.Contains("POWER", sql, StringComparison.Ordinal);
                    Assert.Contains("NULLIF", sql, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("WHERE 0", sql);
                }
            }
        }

        private static string Sql(TestWidgetContext context, BooleanFunction filter) =>
            context.WhereSql(new FilterCriteria { Expression = filter });

        private static string Sql(NumberContext context, BooleanFunction filter) => context.WhereSql(filter);

        private sealed class NumberRow
        {
            public int Id { get; set; }

            public double Number { get; set; }

            public double? Maybe { get; set; }
        }

        private sealed class NumberContext : DbContext
        {
            public NumberContext()
                : base(new DbContextOptionsBuilder<NumberContext>().UseSqlite("Data Source=unused.db").Options)
            {
            }

            public DbSet<NumberRow> Rows => Set<NumberRow>();

            public string WhereSql(BooleanFunction filter)
            {
                var mapping = new LinqQueryMapping<NumberRow>().Field("number", r => r.Number).Field("maybe", r => r.Maybe);
                var transformer = new EfCoreExpressionToLinqTransformer(Database.ProviderName);
                return Rows.Where(transformer, new FilterCriteria { Expression = filter }, mapping).Select(r => r.Id).ToQueryString();
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.HasExpressoFunctions(Database.ProviderName);
                modelBuilder.Entity<NumberRow>().ToTable("number_row");
            }
        }
    }
}
