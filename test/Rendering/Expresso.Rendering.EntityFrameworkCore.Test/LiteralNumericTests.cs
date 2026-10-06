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
        public void Oracle_DoubleLiteralPower_CastsParametersToNumber()
        {
            // Scenario: EF binds double as BINARY_DOUBLE, and POWER(2, 1024) is then infinity.
            // ADO binds that double as NUMBER and raises ORA-01426, so a literal is cast. A BINARY_DOUBLE column is not.
            using var context = TestWidgetContext.Oracle();
            var literals = Sql(context, new EqFunc(new PowerFunc(new Literal(2.0), new Literal(1024.0)), new Literal(0.0)));
            var column = Sql(context, new EqFunc(new PowerFunc(new Field("amount", typeof(double)), new Literal(0.5)), new Literal(2.0)));
            Assert.Contains("POWER(CAST(", literals.Replace(" ", ""), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("NUMBER", literals, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("POWER(CAST(", column.Replace(" ", ""), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SqlServer_IntegerPower_KeepsTheBaseType()
        {
            // Scenario: SQL Server POWER of an int returns int. Casting the base to float changes 5^0.5 from 2 to about 2.236.
            using var context = TestWidgetContext.SqlServer();
            var fractional = Sql(context, new EqFunc(new PowerFunc(new Field("age", typeof(int)), new Literal(0.5)), new Literal(2.0)));
            var negative = Sql(context, new EqFunc(new PowerFunc(new Field("age", typeof(int)), new Literal(-1.0)), new Literal(0.0)));
            var code = Sql(context, new EqFunc(new PowerFunc(new Field("code", typeof(byte)), new Literal(0.5)), new Literal(2.0)));
            var wide = Sql(context, new EqFunc(new PowerFunc(new Field("age", typeof(int)), new Literal(40)), new Literal(0.0)));
            var amount = Sql(context, new EqFunc(new PowerFunc(new Field("amount", typeof(double)), new Literal(0.5)), new Literal(2.0)));
            AssertIntegerPower(fractional);
            AssertIntegerPower(negative);
            AssertIntegerPower(code);
            AssertIntegerPower(wide);
            Assert.Contains("POWER(", amount, StringComparison.Ordinal);
            Assert.DoesNotContain("CAST(POWER(", amount, StringComparison.Ordinal);

            var nested = Sql(context, new EqFunc(
                new PowerFunc(new PowerFunc(new Field("age", typeof(int)), new Literal(0.5)), new Literal(0.5)),
                new Literal(1.0)));
            var divided = Sql(context, new EqFunc(
                new DivFunc(new PowerFunc(new Field("age", typeof(int)), new Literal(1)), new Literal(2)),
                new Literal(2.0)));
            var absolute = Sql(context, new EqFunc(new AbsFunc(new PowerFunc(new Literal(-2), new Literal(31))), new Literal(0)));
            AssertNoInnerCast(nested);
            Assert.Contains("POWER(POWER(", nested.Replace(" ", ""), StringComparison.Ordinal);
            AssertNoInnerCast(divided);
            Assert.Contains("/", divided);
            AssertNoInnerCast(absolute);
            Assert.Contains("ABS(", absolute.Replace(" ", ""), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SqlServer_IntegerFloorCeilingAndRound_StayInteger()
        {
            // Scenario: FLOOR, CEILING and ROUND of an int stay int. Casting to float makes the following division 2.5 instead of 2.
            using var context = TestWidgetContext.SqlServer();
            using var other = TestWidgetContext.Sqlite();
            var age = new Field("age", typeof(int));
            var code = new Field("code", typeof(byte));
            var amount = new Field("amount", typeof(double));
            foreach (var wrap in new Func<AbstractExpression, AbstractExpression>[]
            {
                value => new FloorFunc(value),
                value => new CeilingFunc(value),
                value => new RoundFunc(value, new Literal(0)),
            })
            {
                AssertIntegerDivision(Sql(context, Divided(wrap(new PowerFunc(age, new Literal(1))))));
                AssertIntegerDivision(Sql(context, Divided(wrap(age))));
                AssertIntegerDivision(Sql(context, Divided(wrap(code))));
                AssertIntegerDivision(Sql(context, Divided(wrap(new Literal(5)))));
                var floating = Sql(context, new EqFunc(new DivFunc(wrap(amount), new Literal(2.0)), new Literal(2.5)));
                Assert.Contains("Amount", floating, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("/", floating);
                var sqlite = Sql(other, Divided(wrap(age)));
                Assert.Contains("CAST", sqlite, StringComparison.OrdinalIgnoreCase);
            }

            using var ranks = new RankContext();
            var nullable = ranks.WhereSql(Divided(new FloorFunc(new Field("rank", typeof(int)))));
            AssertIntegerDivision(nullable);
            Assert.Contains("NULL", nullable, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Sqlite_LiteralArithmetic_StaysInSql()
        {
            // Scenario: the CLR wraps int addition and multiplication. SQLite uses a wider range, so the command must add in SQL.
            using var context = TestWidgetContext.Sqlite();
            var sum = Sql(context, new GtFunc(new AddFunc(new Literal(int.MaxValue), new Literal(1)), new Literal(0)));
            var difference = Sql(context, new EqFunc(new SubFunc(new Literal(int.MinValue), new Literal(1)), new Literal(0)));
            var product = Sql(context, new EqFunc(new MultFunc(new Literal(65536), new Literal(65536)), new Literal(0)));
            var missing = Sql(context, new IsNullFunc(new AddFunc(new Literal(int.MaxValue), new Literal(1))));
            Assert.Contains("+", sum);
            Assert.Contains("+", missing);
            Assert.Contains("-", difference);
            Assert.Contains("*", product);
            Assert.DoesNotContain("WHERE @", sum);
            Assert.DoesNotContain("WHERE @", product);
        }

        [Fact]
        public void IsNullAbsAndSubstring_KeepTheCall()
        {
            // Scenario: a non-nullable int abs and a negative substring length can fail. Dropping the call makes isnull false.
            foreach (var context in new[] { TestWidgetContext.SqlServer(), TestWidgetContext.Sqlite(), TestWidgetContext.Oracle() })
            {
                using (context)
                {
                    var absolute = Sql(context, new IsNullFunc(new AbsFunc(new Field("age", typeof(int)))));
                    var minimum = Sql(context, new IsNullFunc(new AbsFunc(new Literal(int.MinValue))));
                    var negated = Sql(context, new NotFunc(new IsNullFunc(new AbsFunc(new Field("age", typeof(int))))));
                    var slice = Sql(context, new IsNullFunc(new SubStringFunc(new Field("name", typeof(string)), new Literal(1), new Literal(-1))));
                    var negatedSlice = Sql(context, new NotFunc(new IsNullFunc(new SubStringFunc(new Field("name", typeof(string)), new Literal(1), new Literal(-1)))));
                    Assert.Contains("ABS", absolute, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("ABS", minimum, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("ABS", negated, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("SUBSTR", slice, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("SUBSTR", negatedSlice, StringComparison.OrdinalIgnoreCase);
                    if (context.Database.ProviderName?.Contains("SqlServer", StringComparison.Ordinal) == true)
                    {
                        var left = Sql(context, new IsNullFunc(new LeftFunc(new Field("name", typeof(string)), new Literal(-1))));
                        var right = Sql(context, new NotFunc(new IsNullFunc(new RightFunc(new Field("name", typeof(string)), new Literal(-1)))));
                        var literal = Sql(context, new IsNullFunc(new LeftFunc(new Literal("ab"), new Literal(-1))));
                        var notes = Sql(context, new IsNullFunc(new RightFunc(new Field("notes", typeof(string)), new Literal(-1))));
                        Assert.True(left.Contains("LEFT", StringComparison.OrdinalIgnoreCase) || left.Contains("SUBSTRING", StringComparison.OrdinalIgnoreCase), left);
                        Assert.True(right.Contains("RIGHT", StringComparison.OrdinalIgnoreCase) || right.Contains("SUBSTRING", StringComparison.OrdinalIgnoreCase), right);
                        Assert.True(literal.Contains("LEFT", StringComparison.OrdinalIgnoreCase) || literal.Contains("SUBSTRING", StringComparison.OrdinalIgnoreCase), literal);
                        Assert.Contains("NULL", notes, StringComparison.OrdinalIgnoreCase);
                        Assert.True(notes.Contains("RIGHT", StringComparison.OrdinalIgnoreCase) || notes.Contains("SUBSTRING", StringComparison.OrdinalIgnoreCase), notes);
                    }

                    Assert.DoesNotContain("WHERE 0", absolute);
                    Assert.DoesNotContain("WHERE 0", minimum);
                }
            }
        }

        private static EqFunc Divided(AbstractExpression value) =>
            new(new DivFunc(value, new Literal(2)), new Literal(2));

        private static void AssertIntegerDivision(string sql)
        {
            var compact = sql.Replace(" ", "");
            Assert.True(
                compact.Contains("/", StringComparison.Ordinal)
                && (compact.Contains("FLOOR(", StringComparison.OrdinalIgnoreCase)
                    || compact.Contains("CEILING(", StringComparison.OrdinalIgnoreCase)
                    || compact.Contains("ROUND(", StringComparison.OrdinalIgnoreCase))
                && !compact.Contains("ASfloat", StringComparison.OrdinalIgnoreCase)
                && !compact.Contains("POWER(CAST(", StringComparison.OrdinalIgnoreCase),
                sql);
        }

        private static void AssertIntegerPower(string sql)
        {
            Assert.Contains("CAST(POWER(", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("POWER(CAST(", sql, StringComparison.Ordinal);
        }

        private static void AssertNoInnerCast(string sql)
        {
            var compact = sql.Replace(" ", "");
            Assert.True(
                compact.Contains("POWER(", StringComparison.Ordinal)
                && !compact.Contains("POWER(CAST(", StringComparison.Ordinal)
                && !compact.Contains("ASfloat)/", StringComparison.OrdinalIgnoreCase)
                && !compact.Contains("ABS(CAST(", StringComparison.OrdinalIgnoreCase)
                && !compact.Contains("ASfloat),", StringComparison.OrdinalIgnoreCase),
                sql);
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

        private sealed class RankRow
        {
            public int Id { get; set; }

            public int? Rank { get; set; }
        }

        private sealed class RankContext : DbContext
        {
            public RankContext()
                : base(new DbContextOptionsBuilder<RankContext>().UseSqlServer("Server=unused;Database=unused").Options)
            {
            }

            public DbSet<RankRow> Rows => Set<RankRow>();

            public string WhereSql(BooleanFunction filter)
            {
                var mapping = new LinqQueryMapping<RankRow>().Field("rank", r => r.Rank);
                var transformer = new EfCoreExpressionToLinqTransformer(Database.ProviderName);
                return Rows.Where(transformer, new FilterCriteria { Expression = filter }, mapping).Select(r => r.Id).ToQueryString();
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.HasExpressoFunctions(Database.ProviderName);
                modelBuilder.Entity<RankRow>().ToTable("rank_row");
            }
        }

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
