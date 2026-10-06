using System.Linq.Expressions;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Rendering.Linq;
using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test
{
    /// <summary>Oracle <c>DateOnly</c>/<c>TimeOnly</c> SQL with the sample's DATE and INTERVAL converters (no connection).</summary>
    public sealed class OracleTemporalTests
    {
        [Fact]
        public void NativeStorage_TranslatesHourAndNestedAddSeconds()
        {
            using var context = new NativeOracleContext();
            var transformer = new EfCoreExpressionToLinqTransformer(context.Database.ProviderName);
            var nested = new FilterCriteria
            {
                Expression = new EqFunc(
                    new HourFunc(new AddSecondsFunc(new Field("opens", typeof(TimeOnly)), new Literal(10))),
                    new Literal(9)),
            };

            var sql = context.Rows.Where(transformer, nested, TemporalMapping.Create()).ToQueryString();

            Assert.Contains("NUMTODSINTERVAL", sql);
            Assert.Contains("REGEXP_SUBSTR", sql);
            Assert.DoesNotContain("NVARCHAR2", sql);
        }

        [Fact]
        public void NativeStorage_TranslatesDateOnlyYear()
        {
            using var context = new NativeOracleContext();
            var transformer = new EfCoreExpressionToLinqTransformer(context.Database.ProviderName);
            var filter = new FilterCriteria
            {
                Expression = new EqFunc(new YearFunc(new Field("born", typeof(DateOnly))), new Literal(2020)),
            };

            var sql = context.Rows.Where(transformer, filter, TemporalMapping.Create()).ToQueryString();

            Assert.Contains("TO_CHAR", sql);
            Assert.Contains("YYYY", sql);
            Assert.DoesNotContain("NVARCHAR2", sql);

            var shifted = new FilterCriteria
            {
                Expression = new EqFunc(
                    new YearFunc(new AddYearsFunc(new Field("born", typeof(DateOnly)), new Literal(1))),
                    new Literal(2021)),
            };
            var shiftedSql = context.Rows.Where(transformer, shifted, TemporalMapping.Create()).ToQueryString();
            Assert.Contains("NUMTOYMINTERVAL", shiftedSql);
            Assert.DoesNotContain("ADD_MONTHS", shiftedSql);
        }

        [Fact]
        public void NativeStorage_ComposesDateAndTimeConversions()
        {
            using var context = new NativeOracleContext();
            var transformer = new EfCoreExpressionToLinqTransformer(context.Database.ProviderName);
            var year = new FilterCriteria
            {
                Expression = new EqFunc(new YearFunc(new DateFunc(new Field("created", typeof(DateTime)))), new Literal(2026)),
            };
            var hour = new FilterCriteria
            {
                Expression = new EqFunc(new HourFunc(new TimeFunc(new Field("created", typeof(DateTime)))), new Literal(9)),
            };
            var added = new FilterCriteria
            {
                Expression = new EqFunc(
                    new HourFunc(new AddSecondsFunc(new TimeFunc(new Field("created", typeof(DateTime))), new Literal(10))),
                    new Literal(9)),
            };

            var yearSql = context.Rows.Where(transformer, year, TemporalMapping.Create()).ToQueryString();
            var hourSql = context.Rows.Where(transformer, hour, TemporalMapping.Create()).ToQueryString();
            var addedSql = context.Rows.Where(transformer, added, TemporalMapping.Create()).ToQueryString();

            Assert.Contains("TRUNC", yearSql);
            Assert.Contains("YYYY", yearSql);
            Assert.Contains("TRUNC", hourSql);
            Assert.DoesNotContain("NUMTODSINTERVAL", hourSql);
            Assert.Contains("NUMTODSINTERVAL", addedSql);
            Assert.Contains("TRUNC", addedSql);
            Assert.Contains("REGEXP_SUBSTR", hourSql);
        }

        [Fact]
        public void NativeStorage_BindsDateAndTimeLiterals()
        {
            using var context = new NativeOracleContext();
            var transformer = new EfCoreExpressionToLinqTransformer(context.Database.ProviderName);
            var created = new Field("created", typeof(DateTime));
            var date = new DateFunc(created);
            var time = new TimeFunc(created);
            FilterCriteria[] filters =
            {
                new() { Expression = new EqFunc(date, new Literal(new DateOnly(2026, 10, 5))) },
                new() { Expression = new GteFunc(date, new Literal(new DateOnly(2026, 10, 5))) },
                new() { Expression = new EqFunc(new AddDaysFunc(date, new Literal(1)), new Literal(new DateOnly(2026, 10, 6))) },
                new() { Expression = new EqFunc(date, new Field("born", typeof(DateOnly))) },
                new() { Expression = new EqFunc(time, new Literal(new TimeOnly(9, 0, 0))) },
                new() { Expression = new EqFunc(time, new Literal(new TimeOnly(9, 0, 0, 500))) },
                new() { Expression = new EqFunc(time, new Field("opens", typeof(TimeOnly))) },
            };

            foreach (var filter in filters)
            {
                var sql = context.Rows.Where(transformer, filter, TemporalMapping.Create()).ToQueryString();
                if (filter.Expression is EqFunc { Arguments: var args } && args[0] is TimeFunc)
                {
                    Assert.Contains("TRUNC", sql);
                    Assert.DoesNotContain("'SS'", sql);
                }
            }
        }

        [Fact]
        public void NativeStorage_LiteralLeftAndSubstring_StayInSql()
        {
            using var context = new NativeOracleContext();
            var transformer = new EfCoreExpressionToLinqTransformer(context.Database.ProviderName);
            string Sql(BooleanFunction expression) =>
                context.Rows.Where(transformer, new FilterCriteria { Expression = expression }, TemporalMapping.Create()).ToQueryString();

            var left = Sql(new EqFunc(new LeftFunc(new Literal("a"), new Literal(100)), new Literal("a")));
            var emptyLeft = new IsNullFunc(new LeftFunc(new Literal(""), new Literal(1)));
            var emptySubstring = new IsNullFunc(new SubStringFunc(new Literal(""), new Literal(1), new Literal(1)));

            Assert.Contains("SUBSTR", left);
            Assert.True(new EfCoreExpressionToLinqTransformer(context.Database.ProviderName).BuildPredicate(new FilterCriteria { Expression = emptyLeft }, TemporalMapping.Create()).Body is ConstantExpression { Value: true });
            Assert.True(new EfCoreExpressionToLinqTransformer(context.Database.ProviderName).BuildPredicate(new FilterCriteria { Expression = emptySubstring }, TemporalMapping.Create()).Body is ConstantExpression { Value: true });
            Sql(emptyLeft);
            Sql(emptySubstring);
        }

        [Fact]
        public void NativeStorage_NegativeIntervalHour_KeepsTheSign()
        {
            using var context = new NativeOracleContext();
            var transformer = new EfCoreExpressionToLinqTransformer(context.Database.ProviderName);
            var filter = new FilterCriteria
            {
                Expression = new EqFunc(
                    new HourFunc(new AddHoursFunc(new Field("opens", typeof(TimeOnly)), new Literal(-10))),
                    new Literal(-1)),
            };

            var sql = context.Rows.Where(transformer, filter, TemporalMapping.Create()).ToQueryString();

            Assert.Contains("SUBSTR", sql);
            Assert.Contains("'-'", sql);
        }

        [Fact]
        public void TextStorage_IsRejected()
        {
            using var context = new TextOracleContext();
            var transformer = new EfCoreExpressionToLinqTransformer(context.Database.ProviderName);
            var filter = new FilterCriteria
            {
                Expression = new EqFunc(new HourFunc(new Field("opens", typeof(TimeOnly))), new Literal(9)),
            };

            var ex = Assert.ThrowsAny<Exception>(() => context.Rows.Where(transformer, filter, TemporalMapping.Create()).ToQueryString());

            Assert.Contains("text storage", ex.ToString());
        }

        private sealed class TemporalRow
        {
            public int Id { get; set; }

            public DateOnly? Born { get; set; }

            public TimeOnly Opens { get; set; }

            public DateTime Created { get; set; }
        }

        private static class TemporalMapping
        {
            public static LinqQueryMapping<TemporalRow> Create() =>
                new LinqQueryMapping<TemporalRow>()
                    .Field("born", r => r.Born)
                    .Field("opens", r => r.Opens)
                    .Field("created", r => r.Created);
        }

        private abstract class OracleTemporalContextBase : DbContext
        {
            protected OracleTemporalContextBase(DbContextOptions options)
                : base(options)
            {
            }

            public DbSet<TemporalRow> Rows => Set<TemporalRow>();
        }

        /// <summary>Separate type so EF does not reuse the text-storage model.</summary>
        private sealed class NativeOracleContext : OracleTemporalContextBase
        {
            public NativeOracleContext()
                : base(new DbContextOptionsBuilder<NativeOracleContext>().UseOracle("User Id=unused;Password=unused;Data Source=unused").Options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.HasExpressoFunctions(Database.ProviderName);
                modelBuilder.Entity<TemporalRow>(e =>
                {
                    e.Property(r => r.Opens)
                        .HasConversion(v => v.ToTimeSpan(), v => TimeOnly.FromTimeSpan(v))
                        .HasColumnType("INTERVAL DAY(0) TO SECOND(0)");
                    e.Property(r => r.Created).HasColumnType("TIMESTAMP");
                    e.Property(r => r.Born)
                        .HasConversion(
                            d => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                            dt => dt.HasValue ? DateOnly.FromDateTime(dt.Value) : (DateOnly?)null)
                        .HasColumnType("DATE");
                });
            }
        }

        /// <summary>Separate type so EF does not reuse the DATE/INTERVAL model.</summary>
        private sealed class TextOracleContext : OracleTemporalContextBase
        {
            public TextOracleContext()
                : base(new DbContextOptionsBuilder<TextOracleContext>().UseOracle("User Id=unused;Password=unused;Data Source=unused").Options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.HasExpressoFunctions(Database.ProviderName);
            }
        }
    }
}
