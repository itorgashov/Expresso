#if NETFRAMEWORK
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using Expresso.Rendering.EntityFramework;

namespace Expresso.Rendering.Integration.Test.Ef6
{
    /// <summary>
    /// Maps the <c>widget*</c> tables created by the ADO fixtures (no schema creation). Column types keep the stored
    /// representation the ADO seed wrote.
    /// </summary>
    [DbConfigurationType(typeof(ItEf6Configuration))]
    public sealed class WidgetEf6Context : DbContext, IDbModelCacheKeyProvider
    {
        private readonly string? _schema;

        public WidgetEf6Context(DbConnection connection, string? schema)
            : base(connection, contextOwnsConnection: true)
        {
            _schema = schema;
        }

        public DbSet<Widget> Widgets => Set<Widget>();

        public DbSet<WidgetTag> Tags => Set<WidgetTag>();

        public string CacheKey => Database.Connection.GetType().FullName + "|" + _schema;

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var provider = Ef6Providers.Resolve(Database.Connection);
            if (_schema is not null)
            {
                modelBuilder.HasDefaultSchema(_schema);
            }

            if (provider == Ef6Provider.Oracle)
            {
                modelBuilder.Properties<string>().Configure(p => p.IsUnicode(false));
            }

            var widget = modelBuilder.Entity<Widget>().ToTable("widget");
            widget.HasKey(w => w.Id).Property(w => w.Id).HasColumnName("id").HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            widget.Property(w => w.Name).HasColumnName("name");
            widget.Property(w => w.Age).HasColumnName("age");
            widget.Property(w => w.Amount).HasColumnName("amount");
            widget.Property(w => w.Active).HasColumnName("active");
            widget.Property(w => w.Created).HasColumnName("created_at");
            widget.Property(w => w.ExternalId).HasColumnName("external_id");
            widget.Property(w => w.Notes).HasColumnName("notes");
            widget.Property(w => w.Code).HasColumnName("code");
            widget.HasMany(w => w.Tags).WithRequired().HasForeignKey(t => t.WidgetId);

            // Oracle's and SQLite's EF6 providers have no store type for a time-of-day TimeSpan.
            if (provider is Ef6Provider.Oracle or Ef6Provider.Sqlite)
            {
                widget.Ignore(w => w.Opens);
            }
            else
            {
                widget.Property(w => w.Opens).HasColumnName("opens");
            }

            switch (provider)
            {
                case Ef6Provider.SqlServer:
                    widget.Property(w => w.Created).HasColumnType("datetime2");
                    break;
                case Ef6Provider.PostgreSql:
                    widget.Property(w => w.Created).HasColumnType("timestamp");
                    widget.Property(w => w.Opens).HasColumnType("time");
                    break;
                case Ef6Provider.Oracle:
                    widget.Property(w => w.Created).HasColumnType("timestamp");
                    break;
            }

            var tag = modelBuilder.Entity<WidgetTag>().ToTable("widget_tag");
            tag.HasKey(t => t.Id).Property(t => t.Id).HasColumnName("id").HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            tag.Property(t => t.WidgetId).HasColumnName("widget_id");
            tag.Property(t => t.Label).HasColumnName("label");
            tag.Property(t => t.Score).HasColumnName("score");
            tag.HasMany(t => t.TagMeta).WithRequired().HasForeignKey(m => m.TagId);

            var meta = modelBuilder.Entity<WidgetTagMeta>().ToTable("widget_tag_meta");
            meta.HasKey(m => m.Id).Property(m => m.Id).HasColumnName("id").HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            meta.Property(m => m.TagId).HasColumnName("tag_id");
            meta.Property(m => m.Kind).HasColumnName("kind");
            meta.Property(m => m.Value).HasColumnName("value");
        }
    }
}
#endif
