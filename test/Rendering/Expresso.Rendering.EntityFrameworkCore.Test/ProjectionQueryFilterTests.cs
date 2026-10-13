using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed class ProjectionQueryFilterTests
{
    [Theory]
    [MemberData(nameof(Scopes))]
    public async Task CallerScope_RetainsOrIgnoresGlobalFilters_InEveryProjectionQuery(
        string provider, bool ignoreFilters, bool explicitKeySource)
    {
        // Scenario: global filters at three levels; inspect actual SQL, not the memory graph's unfiltered results.
        var options = new DbContextOptionsBuilder<FilterContext>();
        switch (provider)
        {
            case "SqlServer": options.UseSqlServer("Server=unused;Database=unused"); break;
            case "PostgreSql": options.UseNpgsql("Host=unused;Database=unused"); break;
            case "MySql": options.UseMySql("Server=unused;Database=unused", new MySqlServerVersion(new Version(8, 0, 36))); break;
            case "Sqlite": options.UseSqlite("Data Source=unused.db"); break;
            case "Oracle": options.UseOracle("User Id=unused;Password=unused;Data Source=unused"); break;
        }
        using var db = new FilterContext(options.Options);
        var commands = new List<string>();
        var executor = new ProjectionSqlExecutor(q => commands.Add(q.ToQueryString()));
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var scope = ignoreFilters ? db.Parents.IgnoreQueryFilters() : db.Parents;
        scope = scope.Where(p => p.Tenant == 1);
        await transformer.RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping())
            .ExecuteAsync(scope.OrderBy(p => p.Id).Take(1), executor, keySource: explicitKeySource ? scope : null);
        Assert.Equal(3, commands.Count);
        Assert.All(commands, command =>
        {
            if (ignoreFilters) Assert.DoesNotContain("deleted", command);
            else Assert.Contains("deleted", command);
        });
    }

    public static IEnumerable<object[]> Scopes()
    {
        foreach (var provider in new[] { "SqlServer", "PostgreSql", "MySql", "Sqlite", "Oracle" })
        foreach (var ignore in new[] { false, true })
        foreach (var explicitSource in new[] { false, true })
            yield return new object[] { provider, ignore, explicitSource };
    }

    private sealed class FilterContext : DbContext
    {
        internal FilterContext(DbContextOptions<FilterContext> options) : base(options) { }
        internal DbSet<ProjectionParent> Parents => Set<ProjectionParent>();
        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<ProjectionParent>().HasKey(p => new { p.Tenant, p.Id });
            model.Entity<ProjectionParent>().HasQueryFilter(p => p.Title != "deleted");
            model.Entity<ProjectionChild>().HasKey(p => new { p.Tenant, p.Id });
            model.Entity<ProjectionChild>().HasQueryFilter(p => p.Name != "deleted");
            model.Entity<ProjectionLeaf>().HasKey(p => new { p.Tenant, p.Id });
            model.Entity<ProjectionLeaf>().HasQueryFilter(p => p.Value != "deleted");
            model.Entity<ProjectionParent>().HasMany(p => p.Children).WithOne().HasForeignKey(p => new { p.Tenant, p.ParentId });
            model.Entity<ProjectionChild>().HasMany(p => p.Leaves).WithOne().HasForeignKey(p => new { p.Tenant, p.ChildId });
        }
    }
}
