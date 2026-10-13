#if NET8_0_OR_GREATER
using Expresso.Core.Projection;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.Integration.Test.Ef;

/// <summary>Executes query-provenance regressions in private integration-owned SQLite tables.</summary>
[Trait("Category", "Integration")]
public sealed partial class ProjectionCompositionItTests
{
    [SkippableTheory]
    [MemberData(nameof(Scopes))]
    public async Task Projection_Composition_PreservesCurrentGraphs(string shape, bool explicitKeys, bool adapter)
    {
        // Scenario: valid query-defined collections and EF.Property remain supported; verify real split-read attachment and sparse output.
        Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        ProjectionDdl.Reset(connection, ParameterBinder.At);
        using var db = new Context(new DbContextOptionsBuilder<Context>().UseSqlite(connection).Options);
        var parents = db.Set<ProjectionParent>();
        var source = shape switch
        {
            "property" => parents.Where(p => EF.Property<List<ProjectionChild>>(p, "Children").Any()),
            "left" => parents.GroupJoin(db.Set<ProjectionChild>(), p => new { p.Tenant, p.Id }, c => new { c.Tenant, Id = c.ParentId },
                (p, group) => new { Parent = p, Kids = group }).SelectMany(x => x.Kids.DefaultIfEmpty(), (x, _) => x.Parent),
            "projected" => parents.Select(p => new { Parent = p, Kids = p.Children }).Where(x => x.Kids.Any()).Select(x => x.Parent),
            _ => parents.AsQueryable()
        };
        var mapping = ProjectionFixture.Mapping();
        var projection = ProjectionFixture.Nested();
        if (shape == "mapping")
        {
            var leaf = new LinqProjectionMapping<ProjectionLeaf, ProjectionLeafDto>().Key("tenant", l => l.Tenant).Key("id", l => l.Id)
                .Field("value", "value", l => l.Value, l => l.Value);
            var child = new LinqProjectionMapping<ProjectionChild, ProjectionChildDto>().Key("tenant", c => c.Tenant).Key("id", c => c.Id)
                .Field("name", "name", c => c.Name, c => c.Name)
                .Collection("leaves", "leaves", c => EF.Property<List<ProjectionLeaf>>(c, "Leaves"), c => c.Leaves, leaf, new LinqQueryMapping<ProjectionLeaf>());
            mapping = new LinqProjectionMapping<ProjectionParent, ProjectionParentDto>().Key("tenant", p => p.Tenant).Key("id", p => p.Id)
                .Field("title", "title", p => p.Title, p => p.Title)
                .Collection("children", "children", p => EF.Property<List<ProjectionChild>>(p, "Children"), p => p.Children, child, new LinqQueryMapping<ProjectionChild>());
        }
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var result = adapter
            ? await source.ToProjectedListAsync(transformer, projection, mapping, keySource: explicitKeys ? parents : null)
            : await transformer.RenderProjection(projection, mapping).ExecuteAsync(source, new Executor(), keySource: explicitKeys ? parents : null);
        Assert.Equal(shape == "left" ? 4 : shape == "mapping" ? 3 : 2, result.Items.Count);
        Assert.All(result.Items.Where(p => p.Title == "one"), p =>
        {
            Assert.Equal(new[] { "alpha", "beta" }, p.Children.Select(c => c.Name).OrderBy(n => n));
            Assert.Equal("first", Assert.Single(p.Children.SelectMany(c => c.Leaves)).Value);
        });
        Assert.Equal("second", Assert.Single(result.Items.Single(p => p.Title == "two").Children.SelectMany(c => c.Leaves)).Value);
        Assert.All(result.Items.Where(p => p.Title == "empty"), p => Assert.Empty(p.Children));
        Assert.All(ProjectionShaper.ToSparseObjects(result), row => Assert.DoesNotContain("notes", row.Keys));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    public static IEnumerable<object[]> Scopes()
    {
        foreach (var shape in new[] { "property", "left", "projected", "mapping" })
        foreach (var explicitKeys in new[] { false, true })
        foreach (var adapter in new[] { false, true })
            yield return new object[] { shape, explicitKeys, adapter };
    }

    private sealed class Context : DbContext
    {
        internal Context(DbContextOptions<Context> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder model) => ProjectionModelConfiguration.Configure(model);
    }

    private sealed class Executor : IProjectionQueryExecutor
    {
        public async Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) =>
            await query.ToListAsync(cancellationToken);
    }
}
#endif
