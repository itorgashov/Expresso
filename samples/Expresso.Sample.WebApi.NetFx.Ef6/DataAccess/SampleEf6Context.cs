using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using Expresso.Rendering.EntityFramework;
using Expresso.Sample.WebApi.NetFx.Ef6.Entities;

namespace Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

[DbConfigurationType(typeof(SampleEf6Configuration))]
public sealed class SampleEf6Context : DbContext, IDbModelCacheKeyProvider
{
    private readonly string? _schema;

    public SampleEf6Context(DbConnection connection, string? schema)
        : base(connection, contextOwnsConnection: true)
    {
        _schema = schema;
    }

    public DbSet<Book> Books => Set<Book>();

    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Publisher> Publishers => Set<Publisher>();

    public DbSet<Award> Awards => Set<Award>();

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

        var publisher = modelBuilder.Entity<Publisher>().ToTable("publisher");
        publisher.HasKey(p => p.Id);
        publisher.Property(p => p.Id).HasColumnName("id").HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
        publisher.Property(p => p.Name).HasColumnName("name");
        publisher.Property(p => p.Country).HasColumnName("country");
        publisher.Property(p => p.Location).HasColumnName("location");
        if (provider is Ef6Provider.Oracle or Ef6Provider.Sqlite)
        {
            publisher.Ignore(p => p.OpensAt);
            publisher.Ignore(p => p.ClosesAt);
        }

        if (provider == Ef6Provider.Sqlite)
        {
            publisher.Property(p => p.OpensAtText).HasColumnName("opens_at");
            publisher.Property(p => p.ClosesAtText).HasColumnName("closes_at");
        }
        else if (provider is not Ef6Provider.Oracle)
        {
            publisher.Property(p => p.OpensAt).HasColumnName("opens_at");
            publisher.Property(p => p.ClosesAt).HasColumnName("closes_at");
            publisher.Ignore(p => p.OpensAtText);
            publisher.Ignore(p => p.ClosesAtText);
            if (provider == Ef6Provider.PostgreSql)
            {
                publisher.Property(p => p.OpensAt).HasColumnType("time");
                publisher.Property(p => p.ClosesAt).HasColumnType("time");
            }
        }
        else
        {
            publisher.Ignore(p => p.OpensAtText);
            publisher.Ignore(p => p.ClosesAtText);
        }

        var author = modelBuilder.Entity<Author>().ToTable("author");
        author.HasKey(a => a.Id);
        author.Property(a => a.Id).HasColumnName("id").HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
        author.Property(a => a.FirstName).HasColumnName("first_name");
        author.Property(a => a.LastName).HasColumnName("last_name");
        author.Property(a => a.DisplayName).HasColumnName("display_name");
        author.Property(a => a.DateOfBirth).HasColumnName("date_of_birth");
        author.Property(a => a.CreatedAt).HasColumnName("created_at");
        if (provider == Ef6Provider.PostgreSql)
        {
            author.Property(a => a.CreatedAt).HasColumnType("timestamp");
        }

        if (provider == Ef6Provider.SqlServer)
        {
            author.Property(a => a.CreatedAt).HasColumnType("datetime2");
        }

        var book = modelBuilder.Entity<Book>().ToTable("book");
        book.HasKey(b => b.Id);
        book.Property(b => b.Id).HasColumnName("id").HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
        book.Property(b => b.Title).HasColumnName("title");
        book.Property(b => b.Year).HasColumnName("year");
        book.Property(b => b.Isbn).HasColumnName("isbn");
        book.Property(b => b.PublisherId).HasColumnName("publisher_id");
        book.Property(b => b.Rating).HasColumnName("rating");
        book.Property(b => b.Price).HasColumnName("price");
        book.Property(b => b.CreatedAt).HasColumnName("created_at");
        book.Property(b => b.ExternalId).HasColumnName("external_id");
        book.HasRequired(b => b.Publisher).WithMany(p => p.Books).HasForeignKey(b => b.PublisherId);
        book.HasMany(b => b.Authors)
            .WithMany(a => a.Books)
            .Map(m => m.ToTable("book_author").MapLeftKey("book_id").MapRightKey("author_id"));
        if (provider == Ef6Provider.PostgreSql)
        {
            book.Property(b => b.CreatedAt).HasColumnType("timestamp");
        }

        if (provider == Ef6Provider.SqlServer)
        {
            book.Property(b => b.CreatedAt).HasColumnType("datetime2");
        }

        var award = modelBuilder.Entity<Award>().ToTable("award");
        award.HasKey(a => a.Id);
        award.Property(a => a.Id).HasColumnName("id").HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
        award.Property(a => a.AuthorId).HasColumnName("author_id");
        award.Property(a => a.Title).HasColumnName("title");
        award.Property(a => a.Year).HasColumnName("year");
        award.HasRequired(a => a.Author).WithMany(a => a.Awards).HasForeignKey(a => a.AuthorId);
    }
}
