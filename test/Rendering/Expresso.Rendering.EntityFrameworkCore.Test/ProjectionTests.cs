using Expresso.Core.Projection;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;


namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed class ProjectionTests
{
    [Fact]
    public async Task AsyncAdapter_ExecutesRootAndEveryRequestedCollection()
    {
        var source = new AsyncProjectionQuery<ProjectionParent>(ProjectionFixture.Data());
        var result = await source.ToProjectedListAsync(new QueryableExpressionToLinqTransformer(), ProjectionFixture.Nested(), ProjectionFixture.Mapping());
        Assert.Equal("second", result.Items[1].Children[0].Leaves[0].Value);
        Assert.Equal(2, result.GetKey(1)[0]);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.ToProjectedListAsync(new QueryableExpressionToLinqTransformer(),
            ProjectionFixture.Nested(), ProjectionFixture.Mapping(), cancellationToken: new CancellationToken(true)));
    }
    [Fact]
    public async Task ProviderTranslatesCompositeKeyEnvelopesAndNestedQueries()
    {
        // Scenario: the real provider must translate every projection; constructed data verifies attachment without I/O.
        using var db = new ProjectionContext();
        var sql = new List<string>();
        var executor = new ProjectionSqlExecutor(q => sql.Add(q.ToQueryString()));
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var result = await transformer.RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping())
            .ExecuteAsync(db.Parents, executor, transformer);
        Assert.Equal(3, sql.Count);
        Assert.DoesNotContain("Notes", sql[0]);
        Assert.Contains("Title", sql[0]);
        Assert.Contains("Tenant", sql[0]);
        Assert.Equal("first", result.Items[0].Children[0].Leaves[0].Value);
        Assert.Equal("second", result.Items[1].Children[0].Leaves[0].Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WindowShift_DoesNotReapplyRootWindowOrFilterToDescendants(bool explicitKeySource)
    {
        // Scenario: a concurrent insert shifts an ordered page after the root read; children still belong to materialized keys.
        using var db = new ProjectionContext();
        var data = ProjectionFixture.Data();
        var commands = new List<string>();
        var reads = 0;
        var executor = new ProjectionSqlExecutor<ProjectionParent>(data.AsQueryable(), q => commands.Add(q.ToQueryString()), () =>
        {
            if (reads++ == 0) data.Add(new ProjectionParent { Tenant = 3, Id = 1, Title = "zzz" });
        });
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var query = db.Parents.Where(p => p.Title != "empty").OrderByDescending(p => p.Title).ThenBy(p => p.Tenant).ThenBy(p => p.Id).Skip(1).Take(1);
        var result = await transformer.RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping())
            .ExecuteAsync(query.AsNoTracking(), executor, transformer, keySource: explicitKeySource ? db.Parents : null);
        Assert.Equal("one", Assert.Single(result.Items).Title);
        Assert.Equal("first", result.Items[0].Children.Single(c => c.Name == "beta").Leaves.Single().Value);
        Assert.Equal(3, commands.Count);
        Assert.Contains("empty", commands[0]);
        Assert.All(commands.Skip(1), command =>
        {
            if (explicitKeySource) Assert.DoesNotContain("empty", command);
            Assert.DoesNotContain("OFFSET", command);
            Assert.DoesNotContain("TOP(", command);
            Assert.DoesNotContain("Notes", command);
        });
    }

    [Fact]
    public async Task OneToMany_ExcludesUnselectedXml_ForNarrowAndDefaultProjection()
    {
        // Scenario: the middle entity has XML outside the response mapping; neither default nor narrow descendant reads may compare/select it.
        using var db = new ProjectionContext();
        foreach (var projection in new[] { ProjectionDirective.All, ProjectionFixture.Nested() })
        {
            var commands = new List<string>();
            var executor = new ProjectionSqlExecutor(q => commands.Add(q.ToQueryString()));
            var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
            var result = await transformer.RenderProjection(projection, ProjectionFixture.Mapping()).ExecuteAsync(db.Parents, executor);
            Assert.Equal("first", result.Items[0].Children.Single(c => c.Name == "beta").Leaves.Single().Value);
            Assert.All(commands, command => Assert.DoesNotContain("Payload", command));
            Assert.All(commands, command => Assert.DoesNotContain("DISTINCT", command));
        }
    }

    [Fact]
    public async Task ScalarProjection_OmitsCollectionsAndKeepsSelectedNull()
    {
        using var db = new ProjectionContext();
        var sql = new List<string>();
        var executor = new ProjectionSqlExecutor(q => sql.Add(q.ToQueryString()));
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var directive = new ProjectionDirective(new[] { new ProjectionSelection("notes") });
        var result = await transformer.RenderProjection(directive, ProjectionFixture.Mapping()).ExecuteAsync(db.Parents, executor);
        Assert.Single(sql);
        Assert.DoesNotContain("Title", sql[0]);
        Assert.Null(ProjectionShaper.ToSparseObjects(result)[0]["notes"]);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public void Adapter_ValidatesRequiredArguments()
    {
        var transformer = new QueryableExpressionToLinqTransformer();
        Assert.Throws<ArgumentNullException>(() => { _ = EfCoreProjectionExtensions.ToProjectedListAsync<ProjectionParent, ProjectionParentDto>(
            null!, transformer, ProjectionDirective.All, ProjectionFixture.Mapping()); });
        Assert.Throws<ArgumentNullException>(() => { _ = EfCoreProjectionExtensions.ToProjectedListAsync(
            ProjectionFixture.Data().AsQueryable(), null!, ProjectionDirective.All, ProjectionFixture.Mapping()); });
    }
}


public sealed class ProjectionContext : DbContext
{
    public ProjectionContext() : base(new DbContextOptionsBuilder<ProjectionContext>().UseSqlServer("Server=unused;Database=unused").Options)
    {  }
    public DbSet<ProjectionParent> Parents => Set<ProjectionParent>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<ProjectionParent>().ToTable("widget_projection_parent").HasKey(x => new { x.Tenant, x.Id });
        model.Entity<ProjectionChild>().ToTable("widget_projection_child").HasKey(x => new { x.Tenant, x.Id });
        model.Entity<ProjectionChild>().Property(x => x.Payload).HasColumnType("xml");
        model.Entity<ProjectionLeaf>().ToTable("widget_projection_leaf").HasKey(x => new { x.Tenant, x.Id });
        model.Entity<ProjectionParent>().HasMany(x => x.Children).WithOne()
            .HasForeignKey(x => new { x.Tenant, x.ParentId });
        model.Entity<ProjectionChild>().HasMany(x => x.Leaves).WithOne()
            .HasForeignKey(x => new { x.Tenant, x.ChildId });
    }
}
