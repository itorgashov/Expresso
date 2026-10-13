#if NET8_0_OR_GREATER
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.Integration.Test.Ef;

/// <summary>Executes global-filter regressions against isolated, integration-owned SQLite tables.</summary>
[Trait("Category", "Integration")]
public sealed class ProjectionQueryFilterItTests
{
    [SkippableTheory]
    [MemberData(nameof(Scopes))]
    public async Task Projection_QueryFilters_RespectCallerScopeAtEveryLevel(
        string filteredLevel, bool ignoreFilters, bool adapter)
    {
        // Scenario: soft-delete filters at each depth, composite identities and an authorized page; verify real SQL results.
        Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        ProjectionDdl.Reset(connection, ParameterBinder.At);
        using var db = new FilterContext(new DbContextOptionsBuilder<FilterContext>().UseSqlite(connection).Options, filteredLevel);
        // Only these private in-memory widget tables are changed; no server connection or sample schema is used.
        foreach (var sql in new[]
        {
            "ALTER TABLE widget_projection_parent ADD COLUMN is_deleted INTEGER NOT NULL DEFAULT 0",
            "ALTER TABLE widget_projection_child ADD COLUMN is_deleted INTEGER NOT NULL DEFAULT 0",
            "ALTER TABLE widget_projection_leaf ADD COLUMN is_deleted INTEGER NOT NULL DEFAULT 0",
            "UPDATE widget_projection_parent SET is_deleted = 1 WHERE tenant = 1 AND id = 1",
            "UPDATE widget_projection_child SET is_deleted = 1 WHERE tenant = 1 AND id = 1",
            "UPDATE widget_projection_leaf SET is_deleted = 1 WHERE tenant = 1 AND id = 1"
        })
            await db.Database.ExecuteSqlRawAsync(sql);
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var scope = ignoreFilters ? db.Set<ProjectionParent>().IgnoreQueryFilters() : db.Set<ProjectionParent>();
        scope = scope.Where(p => p.Tenant == 1);
        var page = scope.OrderBy(p => p.Tenant).ThenBy(p => p.Id).Take(2);
        var projection = ProjectionFixture.Nested();
        var mapping = ProjectionFixture.Mapping();
        var result = adapter
            ? await page.ToProjectedListAsync(transformer, projection, mapping, keySource: scope)
            : await transformer.RenderProjection(projection, mapping).ExecuteAsync(page, new Executor(), keySource: scope);

        var filterParent = !ignoreFilters && db.FilterParents;
        Assert.Equal(filterParent ? new[] { "empty" } : new[] { "one", "empty" }, result.Items.Select(p => p.Title));
        Assert.Empty(result.Items.Last().Children);
        if (filterParent) return;
        var parent = result.Items[0];
        Assert.Equal(!ignoreFilters && db.FilterChildren ? new[] { "alpha" } : new[] { "alpha", "beta" },
            parent.Children.Select(c => c.Name).OrderBy(name => name));
        var expectedLeaves = !ignoreFilters && (db.FilterChildren || db.FilterLeaves) ? Array.Empty<string>() : new[] { "first" };
        Assert.Equal(expectedLeaves, parent.Children.SelectMany(c => c.Leaves).Select(l => l.Value));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    public static IEnumerable<object[]> Scopes()
    {
        foreach (var level in new[] { "parent", "child", "leaf", "all" })
        foreach (var ignore in new[] { false, true })
        foreach (var adapter in new[] { false, true })
            yield return new object[] { level, ignore, adapter };
    }

    private sealed class Executor : IProjectionQueryExecutor
    {
        public async Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) =>
            await query.ToListAsync(cancellationToken);
    }

    private sealed class FilterContext : DbContext
    {
        internal FilterContext(DbContextOptions<FilterContext> options, string level) : base(options)
        {
            FilterParents = level is "parent" or "all";
            FilterChildren = level is "child" or "all";
            FilterLeaves = level is "leaf" or "all";
        }
        public bool FilterParents { get; }
        public bool FilterChildren { get; }
        public bool FilterLeaves { get; }

        protected override void OnModelCreating(ModelBuilder model)
        {
            ProjectionModelConfiguration.Configure(model);
            model.Entity<ProjectionParent>().Property<bool>("IsDeleted").HasColumnName("is_deleted");
            model.Entity<ProjectionChild>().Property<bool>("IsDeleted").HasColumnName("is_deleted");
            model.Entity<ProjectionLeaf>().Property<bool>("IsDeleted").HasColumnName("is_deleted");
            model.Entity<ProjectionParent>().HasQueryFilter(p => !FilterParents || !EF.Property<bool>(p, "IsDeleted"));
            model.Entity<ProjectionChild>().HasQueryFilter(c => !FilterChildren || !EF.Property<bool>(c, "IsDeleted"));
            model.Entity<ProjectionLeaf>().HasQueryFilter(l => !FilterLeaves || !EF.Property<bool>(l, "IsDeleted"));
        }
    }
}
#endif
