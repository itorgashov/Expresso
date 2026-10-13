#if NET8_0_OR_GREATER
using Expresso.Core.Projection;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Expresso.Rendering.Integration.Test.Ef;

[Trait("Category", "Integration")]
public sealed class ProjectionReferenceItTests
{
    [SkippableTheory]
    [MemberData(nameof(Scopes))]
    public async Task Projection_ReferencePaths_KeepMappedGraph(bool reference, bool collection, bool mapping, bool explicitKeys, bool adapter)
    {
        // Scenario: a self-reference reaches a mapped collection through all CLR/EF.Property combinations; verify actual attachment.
        Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var db = new Context(new DbContextOptionsBuilder<Context>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync(); // Only this private in-memory widget_projection_reference table.
        await db.Database.ExecuteSqlRawAsync("INSERT INTO widget_projection_reference (Id, Name, ParentId) VALUES (1, 'parent', 1), (2, 'child', 1)");
        var nodes = db.Set<Node>();
        Expression<Func<Node, IEnumerable<Node>>> navigation = reference
            ? collection ? n => EF.Property<List<Node>>(EF.Property<Node>(n, "Parent"), "Children") : n => EF.Property<Node>(n, "Parent").Children
            : collection ? n => EF.Property<List<Node>>(n.Parent!, "Children") : n => n.Parent!.Children;
        var source = mapping ? nodes : nodes.Where(Expression.Lambda<Func<Node, bool>>(
            Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), new[] { typeof(Node) }, navigation.Body), navigation.Parameters));
        var map = new LinqProjectionMapping<Node, Dto>().Key("id", n => n.Id).Field("name", "name", n => n.Name, n => n.Name)
            .Collection("children", "children", mapping ? navigation : n => n.Children, n => n.Children,
                new LinqProjectionMapping<Node, Dto>().Key("id", n => n.Id).Field("name", "name", n => n.Name, n => n.Name), new LinqQueryMapping<Node>());
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var result = adapter ? await source.ToProjectedListAsync(transformer, ProjectionDirective.All, map, keySource: explicitKeys ? nodes : null)
            : await transformer.RenderProjection(ProjectionDirective.All, map).ExecuteAsync(source, new Executor(), keySource: explicitKeys ? nodes : null);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Items.Single(n => n.Name == "parent").Children.Count);
        Assert.Equal(mapping ? 2 : 0, result.Items.Single(n => n.Name == "child").Children.Count);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    public static IEnumerable<object[]> Scopes()
    {
        foreach (var reference in new[] { false, true })
        foreach (var collection in new[] { false, true })
        foreach (var mapping in new[] { false, true })
        foreach (var keys in new[] { false, true })
        foreach (var adapter in new[] { false, true })
            yield return new object[] { reference, collection, mapping, keys, adapter };
    }
    private sealed class Context : DbContext
    {
        internal Context(DbContextOptions<Context> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder model)
        {
            var node = model.Entity<Node>().ToTable("widget_projection_reference");
            node.HasKey(n => n.Id);
            node.Property(n => n.Id).ValueGeneratedNever();
            node.HasMany(n => n.Children).WithOne(n => n.Parent).HasForeignKey(n => n.ParentId);
        }
    }
    public sealed class Node
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int? ParentId { get; set; }
        public Node? Parent { get; set; }
        public List<Node> Children { get; set; } = new();
    }
    public sealed class Dto
    {
        public string Name { get; set; } = "";
        public List<Dto> Children { get; set; } = new();
    }
    private sealed class Executor : IProjectionQueryExecutor
    {
        public async Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) => await query.ToListAsync(cancellationToken);
    }
}
#endif
