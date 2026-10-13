#if NET8_0_OR_GREATER
using Expresso.Core.Projection;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.Integration.Test.Ef;

[Trait("Category", "Integration")]
public sealed class ProjectionOwnedSiblingItTests
{
    [SkippableTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Projection_ReusedOwnedTypes_LoadEachActualOwnership(bool both, bool repeated)
    {
        // Scenario: genuine ownership metadata and stored rows share CLR types/ids; repeated owners receive the right sibling graph.
        Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var db = new OwnedContext(new DbContextOptionsBuilder<OwnedContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync(); // Creates only this private database's widget_owned_* tables.
        db.Add(new Parent { Id = 1, Children = new() { new Child { Id = 10, Leaves = new() { new Leaf { Id = 100, Value = "left" } } } },
            OtherChildren = new() { new Child { Id = 10, Leaves = new() { new Leaf { Id = 100, Value = "right" } } } } });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var source = repeated ? db.Set<Parent>().Concat(db.Set<Parent>()) : db.Set<Parent>();
        var projection = both ? ProjectionDirective.All : new ProjectionDirective(new[] { new ProjectionSelection("children") });
        var result = await source.ToProjectedListAsync(new EfCoreExpressionToLinqTransformer(db.Database.ProviderName), projection, Mapping());
        Assert.Equal(repeated ? 2 : 1, result.Items.Count);
        Assert.All(result.Items, parent =>
        {
            Assert.Equal("left", Assert.Single(Assert.Single(parent.Children).Leaves).Value);
            if (both) Assert.Equal("right", Assert.Single(Assert.Single(parent.OtherChildren).Leaves).Value);
            else Assert.Empty(parent.OtherChildren);
        });
        Assert.All(ProjectionShaper.ToSparseObjects(result), row => Assert.Equal(both ? 2 : 1, row.Count));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    private static LinqProjectionMapping<Parent, Parent> Mapping()
    {
        LinqProjectionMapping<Child, Child> Children() => new LinqProjectionMapping<Child, Child>()
            .Key("owner", c => c.OwnerId).Key("id", c => c.Id)
            .Collection("leaves", "leaves", c => c.Leaves, c => c.Leaves,
                new LinqProjectionMapping<Leaf, Leaf>().Key("owner", l => l.OwnerId).Key("child", l => l.ChildId).Key("id", l => l.Id)
                    .Field("value", "value", l => l.Value, l => l.Value), new LinqQueryMapping<Leaf>());
        return new LinqProjectionMapping<Parent, Parent>().Key("id", p => p.Id)
            .Collection("children", "children", p => p.Children, p => p.Children, Children(), new LinqQueryMapping<Child>())
            .Collection("otherchildren", "otherchildren", p => p.OtherChildren, p => p.OtherChildren, Children(), new LinqQueryMapping<Child>());
    }

    private sealed class OwnedContext : DbContext
    {
        internal OwnedContext(DbContextOptions<OwnedContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<Parent>().ToTable("widget_owned_parent").HasKey(p => p.Id);
            model.Entity<Parent>().Property(p => p.Id).ValueGeneratedNever();
            foreach (var name in new[] { nameof(Parent.Children), nameof(Parent.OtherChildren) })
                model.Entity<Parent>().OwnsMany<Child>(name, child =>
                {
                    child.ToTable("widget_owned_" + name);
                    child.WithOwner().HasForeignKey(c => c.OwnerId);
                    child.HasKey(c => new { c.OwnerId, c.Id });
                    child.Property(c => c.Id).ValueGeneratedNever();
                    child.OwnsMany(c => c.Leaves, leaf =>
                    {
                        leaf.ToTable("widget_owned_" + name + "_leaf");
                        leaf.WithOwner().HasForeignKey(l => new { l.OwnerId, l.ChildId });
                        leaf.HasKey(l => new { l.OwnerId, l.ChildId, l.Id });
                        leaf.Property(l => l.Id).ValueGeneratedNever();
                    });
                });
        }
    }
    public sealed class Parent
    {
        public int Id { get; set; }
        public List<Child> Children { get; set; } = new();
        public List<Child> OtherChildren { get; set; } = new();
    }
    public sealed class Child
    {
        public int OwnerId { get; set; }
        public int Id { get; set; }
        public List<Leaf> Leaves { get; set; } = new();
    }
    public sealed class Leaf
    {
        public int OwnerId { get; set; }
        public int ChildId { get; set; }
        public int Id { get; set; }
        public string Value { get; set; } = "";
    }
}
#endif
