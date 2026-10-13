using Expresso.Core.Projection;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed class ProjectionOwnedSiblingTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ReusedOwnedTypes_RetainEachNavigationAndNestedIdentity(bool both, bool repeated)
    {
        // Scenario: sibling ownerships reuse CLR types/ids; normalize repeated owners, not their already-bound owned navigations.
        using var db = new OwnedContext();
        Assert.Null(db.Model.FindEntityType(typeof(Child)));
        Assert.Equal(2, db.Model.GetEntityTypes().Count(e => e.ClrType == typeof(Child)));
        var commands = new List<string>();
        var graph = new[] { new Parent { Tenant = 1, Id = 1, Children = new() {
            new Child { Tenant = 1, ParentId = 1, Id = 10, Leaves = new() {
                new Leaf { Tenant = 1, ParentId = 1, ChildId = 10, Id = 100, Value = "left" } } } }, OtherChildren = new() {
            new Child { Tenant = 1, ParentId = 1, Id = 10, Leaves = new() {
                new Leaf { Tenant = 1, ParentId = 1, ChildId = 10, Id = 100, Value = "right" } } } } } }.AsQueryable();
        var executor = new ProjectionSqlExecutor<Parent>(graph, q => commands.Add(q.ToQueryString()));
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var source = repeated ? db.Set<Parent>().Concat(db.Set<Parent>()) : db.Set<Parent>();
        var projection = both ? ProjectionDirective.All : new ProjectionDirective(new[] { new ProjectionSelection("children") });
        var result = await transformer.RenderProjection(projection, Mapping()).ExecuteAsync(source, executor);
        Assert.Equal(repeated ? 2 : 1, result.Items.Count);
        Assert.All(result.Items, parent =>
        {
            Assert.Equal("left", Assert.Single(Assert.Single(parent.Children).Leaves).Value);
            if (both) Assert.Equal("right", Assert.Single(Assert.Single(parent.OtherChildren).Leaves).Value);
            else Assert.Empty(parent.OtherChildren);
        });
        Assert.Equal(both ? 5 : 3, commands.Count);
        Assert.All(executor.RowCounts.Skip(1), count => Assert.Equal(1, count));
        Assert.All(commands, command => { Assert.DoesNotContain("GROUP BY", command); Assert.DoesNotContain("DISTINCT", command); });
        Assert.All(ProjectionShaper.ToSparseObjects(result), item => Assert.Equal(both ? 2 : 1, item.Count));
    }

    private static LinqProjectionMapping<Parent, ParentDto> Mapping()
    {
        LinqProjectionMapping<Child, ChildDto> Children() => new LinqProjectionMapping<Child, ChildDto>()
            .Key("tenant", c => c.Tenant).Key("parent", c => c.ParentId).Key("id", c => c.Id)
            .Collection("leaves", "leaves", c => c.Leaves, c => c.Leaves,
                new LinqProjectionMapping<Leaf, ProjectionLeafDto>()
                    .Key("tenant", l => l.Tenant).Key("parent", l => l.ParentId).Key("child", l => l.ChildId).Key("id", l => l.Id)
                    .Field("value", "value", l => l.Value, l => l.Value), new LinqQueryMapping<Leaf>());
        return new LinqProjectionMapping<Parent, ParentDto>().Key("tenant", p => p.Tenant).Key("id", p => p.Id)
            .Collection("children", "children", p => p.Children, p => p.Children, Children(), new LinqQueryMapping<Child>())
            .Collection("otherchildren", "otherchildren", p => p.OtherChildren, p => p.OtherChildren, Children(), new LinqQueryMapping<Child>());
    }

    private sealed class OwnedContext : DbContext
    {
        internal OwnedContext() : base(new DbContextOptionsBuilder<OwnedContext>().UseSqlServer("Server=unused;Database=unused").Options) { }
        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<Parent>().HasKey(p => new { p.Tenant, p.Id });
            foreach (var name in new[] { nameof(Parent.Children), nameof(Parent.OtherChildren) })
                model.Entity<Parent>().OwnsMany<Child>(name, child =>
                {
                    child.ToTable(name);
                    child.WithOwner().HasForeignKey(c => new { c.Tenant, c.ParentId });
                    child.HasKey(c => new { c.Tenant, c.ParentId, c.Id });
                    child.OwnsMany(c => c.Leaves, leaf =>
                    {
                        leaf.ToTable(name + "Leaves");
                        leaf.WithOwner().HasForeignKey(l => new { l.Tenant, l.ParentId, l.ChildId });
                        leaf.HasKey(l => new { l.Tenant, l.ParentId, l.ChildId, l.Id });
                    });
                });
        }
    }

    public sealed class Parent
    {
        public int Tenant { get; set; }
        public int Id { get; set; }
        public List<Child> Children { get; set; } = new();
        public List<Child> OtherChildren { get; set; } = new();
    }
    public sealed class Child
    {
        public int Tenant { get; set; }
        public int ParentId { get; set; }
        public int Id { get; set; }
        public List<Leaf> Leaves { get; set; } = new();
    }
    public sealed class Leaf
    {
        public int Tenant { get; set; }
        public int ParentId { get; set; }
        public int ChildId { get; set; }
        public int Id { get; set; }
        public string Value { get; set; } = "";
    }
    public sealed class ParentDto
    {
        public List<ChildDto> Children { get; set; } = new();
        public List<ChildDto> OtherChildren { get; set; } = new();
    }
    public sealed class ChildDto { public List<ProjectionLeafDto> Leaves { get; set; } = new(); }
}
