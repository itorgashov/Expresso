using Expresso.Core.Projection;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed class ProjectionInheritanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TphDerivedParents_LoadNestedCollections_AndKeepNullablePrizes(bool all)
    {
        // Scenario: shared derived writer with a further subclass; preserve tenant authorization and distinct null-key prizes.
        using var db = new InheritanceContext();
        var commands = new List<string>();
        var executor = new ProjectionSqlExecutor<InheritedBook>(InheritanceProjectionFixture.Data(), q => commands.Add(q.ToQueryString()));
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var result = await transformer.RenderProjection(all ? ProjectionDirective.All : InheritanceProjectionFixture.Nested(),
            InheritanceProjectionFixture.Mapping()).ExecuteAsync(db.Books.Where(x => x.Tenant == 1).OrderBy(x => x.Id).Take(2),
                executor, keySource: db.Books.Where(x => x.Tenant == 1), options: new ProjectionExecutionOptions { MaxKeyParameters = 2 });
        Assert.Equal(new[] { 2, 1, 2, 1, 2 }, executor.RowCounts);
        Assert.All(result.Items, book => Assert.Equal(2, Assert.Single(book.Writers).Awards.Count));
        Assert.All(commands, command => Assert.DoesNotContain("DISTINCT", command));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    private sealed class InheritanceContext : DbContext
    {
        internal InheritanceContext() : base(new DbContextOptionsBuilder<InheritanceContext>().UseSqlServer("Server=unused;Database=unused").Options) { }
        internal DbSet<InheritedBook> Books => Set<InheritedBook>();
        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<InheritedBook>().ToTable("projection_books").HasKey(x => new { x.Tenant, x.Id });
            model.Entity<ProjectionPerson>().ToTable("projection_people").HasKey(x => new { x.Tenant, x.Id });
            model.Entity<ProjectionPerson>().Property(x => x.Bio).HasColumnType("xml");
            model.Entity<ProjectionFeaturedWriter>();
            model.Entity<InheritedPrize>().ToTable("projection_prizes").HasKey(x => new { x.Tenant, x.Id });
            model.Entity<InheritedBook>().HasMany(x => x.Writers).WithMany();
            model.Entity<ProjectionWriter>().HasMany(x => x.Prizes).WithOne().HasForeignKey(x => new { x.Tenant, x.WriterId });
        }
    }
}
