using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.EntityFramework.Test
{
    /// <summary>Widget model on the EF6 SQL Server provider; only <c>DbQuery.ToString</c> is used, so no connection is opened.</summary>
    [DbConfigurationType(typeof(TestEf6Configuration))]
    public sealed class TestWidgetEf6Context : DbContext
    {
        public TestWidgetEf6Context()
            : base(new SqlConnection("Server=unused;Database=unused"), contextOwnsConnection: true)
        {
        }

        public DbSet<Widget> Widgets => Set<Widget>();

        public Ef6ExpressionToLinqTransformer Transformer() => new(this);

        /// <summary>SQL of <c>Widgets.Where(filter).Select(Id)</c>.</summary>
        public string WhereSql(FilterCriteria filter) =>
            Widgets.Where(Transformer(), filter, WidgetLinqMapping.Create()).Select(w => w.Id).ToString();

        /// <summary>SQL of <c>Widgets[.Where(filter)].OrderBy(sort).Select(Id)</c>.</summary>
        public string OrderSql(SortDirective sort, FilterCriteria? filter = null)
        {
            var transformer = Transformer();
            var mapping = WidgetLinqMapping.Create();
            var source = filter is null ? Widgets : Widgets.Where(transformer, filter, mapping);
            return source.OrderBy(transformer, sort, mapping).Select(w => w.Id).ToString();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Widget>().ToTable("widget").Property(w => w.Created).HasColumnName("created_at");
            modelBuilder.Entity<Widget>().HasMany(w => w.Tags).WithRequired().HasForeignKey(t => t.WidgetId);
            modelBuilder.Entity<WidgetTag>().ToTable("widget_tag").HasMany(t => t.TagMeta).WithRequired().HasForeignKey(m => m.TagId);
            modelBuilder.Entity<WidgetTagMeta>().ToTable("widget_tag_meta");
        }
    }

    /// <summary>SQL Server provider with a fixed manifest token, so building the model never connects.</summary>
    public sealed class TestEf6Configuration : DbConfiguration
    {
        public TestEf6Configuration()
        {
            SetProviderServices(SqlProviderServices.ProviderInvariantName, SqlProviderServices.Instance);
            SetManifestTokenResolver(new FixedManifestTokenResolver());
            SetDatabaseInitializer<TestWidgetEf6Context>(null);
        }

        private sealed class FixedManifestTokenResolver : IManifestTokenResolver
        {
            public string ResolveManifestToken(DbConnection connection) => "2012";
        }
    }
}
