using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test
{
    /// <summary>Widget model on a real provider; only <c>ToQueryString</c> is used, so no connection is opened.</summary>
    public sealed class TestWidgetContext : DbContext
    {
        public TestWidgetContext(DbContextOptions<TestWidgetContext> options)
            : base(options)
        {
        }

        public DbSet<Widget> Widgets => Set<Widget>();

        public static TestWidgetContext SqlServer() =>
            new(new DbContextOptionsBuilder<TestWidgetContext>().UseSqlServer("Server=unused;Database=unused").Options);

        public static TestWidgetContext Sqlite() =>
            new(new DbContextOptionsBuilder<TestWidgetContext>().UseSqlite("Data Source=unused.db").Options);

        public static TestWidgetContext Oracle() =>
            new(new DbContextOptionsBuilder<TestWidgetContext>().UseOracle("User Id=unused;Password=unused;Data Source=unused").Options);

        /// <summary>SQL of <c>Widgets.Where(filter).Select(Id)</c> with the transformer for this context's provider.</summary>
        public string WhereSql(FilterCriteria filter)
        {
            var transformer = new EfCoreExpressionToLinqTransformer(Database.ProviderName);
            return Widgets.Where(transformer, filter, WidgetLinqMapping.Create()).Select(w => w.Id).ToQueryString();
        }

        /// <summary>SQL of <c>Widgets.IncludeSorted(sort)</c>.</summary>
        public string IncludeSql(SortDirective sort, LinqQueryMapping<Widget>? mapping = null)
        {
            var transformer = new EfCoreExpressionToLinqTransformer(Database.ProviderName);
            return Widgets.IncludeSorted(transformer, sort, mapping ?? WidgetLinqMapping.Create()).ToQueryString();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasExpressoFunctions(Database.ProviderName);
            modelBuilder.Entity<Widget>(e =>
            {
                e.ToTable("widget");
                e.Property(w => w.Created).HasColumnName("created_at");
                e.HasMany(w => w.Tags).WithOne().HasForeignKey(t => t.WidgetId);
            });
            modelBuilder.Entity<WidgetTag>(e =>
            {
                e.ToTable("widget_tag");
                e.HasMany(t => t.TagMeta).WithOne().HasForeignKey(m => m.TagId);
            });
            modelBuilder.Entity<WidgetTagMeta>().ToTable("widget_tag_meta");
        }
    }
}
