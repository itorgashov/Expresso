#if NET8_0_OR_GREATER
using Expresso.Rendering.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.Integration.Test.Ef
{
    /// <summary>
    /// Maps the <c>widget*</c> tables created by the ADO fixtures (no schema creation). Conversions keep the stored
    /// representation the ADO seed wrote (DB2 <c>Guid</c> text and <c>SMALLINT</c> code, PostgreSQL <c>timestamp</c>/<c>time</c>).
    /// </summary>
    public sealed class WidgetDbContext : DbContext
    {
        public WidgetDbContext(DbContextOptions<WidgetDbContext> options)
            : base(options)
        {
        }

        public DbSet<Widget> Widgets => Set<Widget>();

        public DbSet<WidgetTag> Tags => Set<WidgetTag>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasExpressoFunctions(Database.ProviderName);
            var provider = EfCoreProviders.Resolve(Database.ProviderName);

            modelBuilder.Entity<Widget>(e =>
            {
                e.ToTable("widget");
                e.HasKey(w => w.Id);
                e.Property(w => w.Id).HasColumnName("id").ValueGeneratedNever();
                e.Property(w => w.Name).HasColumnName("name");
                e.Property(w => w.Age).HasColumnName("age");
                e.Property(w => w.Amount).HasColumnName("amount");
                e.Property(w => w.Active).HasColumnName("active");
                e.Property(w => w.Created).HasColumnName("created_at");
                e.Property(w => w.ExternalId).HasColumnName("external_id");
                e.Property(w => w.Opens).HasColumnName("opens");
                e.Property(w => w.Notes).HasColumnName("notes");
                e.Property(w => w.Code).HasColumnName("code");
                e.HasMany(w => w.Tags).WithOne().HasForeignKey(t => t.WidgetId);

                switch (provider)
                {
                    case EfCoreProvider.Db2:
                        e.Property(w => w.ExternalId).HasConversion<string>();
                        e.Property(w => w.Code).HasConversion<short>();
                        break;
                    case EfCoreProvider.PostgreSql:
                        e.Property(w => w.Created).HasColumnType("timestamp without time zone");
                        e.Property(w => w.Opens).HasColumnType("time");
                        break;
                }
            });

            modelBuilder.Entity<WidgetTag>(e =>
            {
                e.ToTable("widget_tag");
                e.HasKey(t => t.Id);
                e.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();
                e.Property(t => t.WidgetId).HasColumnName("widget_id");
                e.Property(t => t.Label).HasColumnName("label");
                e.Property(t => t.Score).HasColumnName("score");
                e.HasMany(t => t.TagMeta).WithOne().HasForeignKey(m => m.TagId);
            });

            modelBuilder.Entity<WidgetTagMeta>(e =>
            {
                e.ToTable("widget_tag_meta");
                e.HasKey(m => m.Id);
                e.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();
                e.Property(m => m.TagId).HasColumnName("tag_id");
                e.Property(m => m.Kind).HasColumnName("kind");
                e.Property(m => m.Value).HasColumnName("value");
            });
        }
    }
}
#endif
