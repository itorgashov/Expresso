using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Sample.WebApi.EfCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Sample.WebApi.EfCore.DataAccess;

public sealed class SampleDbContext : DbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();

    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Publisher> Publishers => Set<Publisher>();

    public DbSet<Award> Awards => Set<Award>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasExpressoFunctions(Database.ProviderName);
        var provider = EfCoreProviders.Resolve(Database.ProviderName);

        if (provider == EfCoreProvider.SqlServer)
        {
            modelBuilder.HasDefaultSchema("dbo");
        }

        modelBuilder.Entity<Publisher>(e =>
        {
            e.ToTable("publisher");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(p => p.Name).HasColumnName("name");
            e.Property(p => p.Country).HasColumnName("country");
            e.Property(p => p.Location).HasColumnName("location");
            e.Property(p => p.OpensAt).HasColumnName("opens_at");
            e.Property(p => p.ClosesAt).HasColumnName("closes_at");
            if (provider == EfCoreProvider.PostgreSql)
            {
                e.Property(p => p.OpensAt).HasColumnType("time");
                e.Property(p => p.ClosesAt).HasColumnType("time");
            }
        });

        modelBuilder.Entity<Author>(e =>
        {
            e.ToTable("author");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(a => a.FirstName).HasColumnName("first_name");
            e.Property(a => a.LastName).HasColumnName("last_name");
            e.Property(a => a.DisplayName).HasColumnName("display_name");
            e.Property(a => a.DateOfBirth).HasColumnName("date_of_birth");
            e.Property(a => a.CreatedAt).HasColumnName("created_at");
            if (provider == EfCoreProvider.PostgreSql)
            {
                e.Property(a => a.CreatedAt).HasColumnType("timestamp without time zone");
            }
        });

        modelBuilder.Entity<Book>(e =>
        {
            e.ToTable("book");
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(b => b.Title).HasColumnName("title");
            e.Property(b => b.Year).HasColumnName("year");
            e.Property(b => b.Isbn).HasColumnName("isbn");
            e.Property(b => b.PublisherId).HasColumnName("publisher_id");
            e.Property(b => b.Rating).HasColumnName("rating");
            e.Property(b => b.Price).HasColumnName("price");
            e.Property(b => b.CreatedAt).HasColumnName("created_at");
            e.Property(b => b.ExternalId).HasColumnName("external_id");
            e.HasOne(b => b.Publisher).WithMany(p => p.Books).HasForeignKey(b => b.PublisherId);
            e.HasMany(b => b.Authors).WithMany(a => a.Books).UsingEntity(
                "book_author",
                l => l.HasOne(typeof(Author)).WithMany().HasForeignKey("author_id"),
                r => r.HasOne(typeof(Book)).WithMany().HasForeignKey("book_id"),
                j => j.ToTable("book_author"));
            if (provider == EfCoreProvider.PostgreSql)
            {
                e.Property(b => b.CreatedAt).HasColumnType("timestamp without time zone");
            }

            if (provider == EfCoreProvider.Db2)
            {
                e.Property(b => b.ExternalId).HasConversion<string>();
            }
        });

        modelBuilder.Entity<Award>(e =>
        {
            e.ToTable("award");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(a => a.AuthorId).HasColumnName("author_id");
            e.Property(a => a.Title).HasColumnName("title");
            e.Property(a => a.Year).HasColumnName("year");
            e.HasOne(a => a.Author).WithMany(a => a.Awards).HasForeignKey(a => a.AuthorId);
        });
    }
}
