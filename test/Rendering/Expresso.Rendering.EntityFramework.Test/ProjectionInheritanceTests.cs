using System.Data.Entity;
using System.Data.SqlClient;
using Expresso.Core.Projection;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.EntityFramework.Test;

public sealed class ProjectionInheritanceTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DerivedParents_LoadNestedCollections_ForTphAndTpt(bool tpt, bool all)
    {
        // Scenario: two books share a derived writer, whose nullable prizes must be read once per key batch without XML equality.
        using InheritanceContext db = tpt ? new TptContext() : new TphContext();
        var commands = new List<string>();
        var executor = new ProjectionSqlExecutor<InheritedBook>(InheritanceProjectionFixture.Data(), q => commands.Add(q.ToString()!));
        var transformer = new Ef6ExpressionToLinqTransformer(db);
        var result = await transformer.RenderProjection(all ? ProjectionDirective.All : InheritanceProjectionFixture.Nested(),
            InheritanceProjectionFixture.Mapping()).ExecuteAsync(db.Books.Where(x => x.Tenant == 1).OrderBy(x => x.Id).Take(2),
                executor, keySource: db.Books.Where(x => x.Tenant == 1), options: new ProjectionExecutionOptions { MaxKeyParameters = 2 });
        Assert.Equal(new[] { 2, 1, 2, 1, 2 }, executor.RowCounts);
        Assert.All(result.Items, book => Assert.Equal(2, Assert.Single(book.Writers).Awards.Count));
        Assert.All(commands, command => Assert.DoesNotContain("DISTINCT", command));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DerivedRoot_AndMultiLevelSubtype_ResolveToTypedSources(bool tpt)
    {
        // Scenario: the root is OfType<Writer>; requesting the leaf subtype must walk both metadata base levels.
        using InheritanceContext db = tpt ? new TptContext() : new TphContext();
        var source = db.People.OfType<ProjectionWriter>().Where(x => x.Tenant == 1);
        var resolved = Assert.IsAssignableFrom<IQueryable<ProjectionFeaturedWriter>>(
            Ef6ProjectionEntitySource.Resolve(source, typeof(ProjectionFeaturedWriter)));
        Assert.Contains("OfType", resolved.Expression.ToString());
        Assert.Contains(tpt ? "projection_featured_writers" : "Discriminator", resolved.ToString());
        var data = InheritanceProjectionFixture.Data().SelectMany(x => x.Writers).Distinct().Cast<ProjectionPerson>()
            .Concat(new[] { new ProjectionPerson { Tenant = 1, Id = 99, Name = "not-writer" } }).AsQueryable();
        var executor = new ProjectionSqlExecutor<ProjectionPerson>(data, q => Assert.DoesNotContain("DISTINCT", q.ToString()));
        var result = await new Ef6ExpressionToLinqTransformer(db).RenderProjection(ProjectionDirective.All, InheritanceProjectionFixture.WriterMapping())
            .ExecuteAsync(source, executor);
        Assert.Equal("chosen", Assert.Single(result.Items).Name);
        Assert.Equal(2, result.Items[0].Awards.Count);
        Assert.Equal(new[] { 1, 2 }, executor.RowCounts);
    }

    [DbConfigurationType(typeof(TestEf6Configuration))]
    public abstract class InheritanceContext : DbContext
    {
        protected InheritanceContext() : base(new SqlConnection("Server=unused;Database=unused"), true) { }
        public DbSet<InheritedBook> Books => Set<InheritedBook>();
        public DbSet<ProjectionPerson> People => Set<ProjectionPerson>();
        protected override void OnModelCreating(DbModelBuilder model)
        {
            model.Entity<InheritedBook>().ToTable("projection_books").HasKey(x => new { x.Tenant, x.Id });
            model.Entity<ProjectionPerson>().ToTable("projection_people").HasKey(x => new { x.Tenant, x.Id });
            model.Entity<ProjectionPerson>().Property(x => x.Bio).HasColumnType("xml");
            model.Entity<ProjectionFeaturedWriter>();
            model.Entity<InheritedPrize>().ToTable("projection_prizes").HasKey(x => new { x.Tenant, x.Id });
            model.Entity<InheritedBook>().HasMany(x => x.Writers).WithMany();
            model.Entity<ProjectionWriter>().HasMany(x => x.Prizes).WithRequired().HasForeignKey(x => new { x.Tenant, x.WriterId });
        }
    }
    public sealed class TphContext : InheritanceContext
    {
        public TphContext() => Database.SetInitializer<TphContext>(null);
    }
    public sealed class TptContext : InheritanceContext
    {
        public TptContext() => Database.SetInitializer<TptContext>(null);
        protected override void OnModelCreating(DbModelBuilder model)
        {
            base.OnModelCreating(model);
            model.Entity<ProjectionWriter>().ToTable("projection_writers");
            model.Entity<ProjectionFeaturedWriter>().ToTable("projection_featured_writers");
        }
    }
}
