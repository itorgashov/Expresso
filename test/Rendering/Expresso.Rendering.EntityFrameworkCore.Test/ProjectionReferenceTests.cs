using Expresso.Core.Projection;
using Expresso.Rendering.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed partial class ProjectionTemporalTests
{
    [Theory]
    [MemberData(nameof(ReferenceScopes))]
    public async Task MappedReferencePaths_RetainCollectionScope(bool efReference, bool efCollection, bool mapped, bool adapter, bool explicitKeys)
    {
        // Scenario: CLR/intrinsic reference and collection combinations produce the same graph through both entry/key modes.
        var noIo = new NoIo();
        using var db = new ReferenceContext(noIo);
        var nodes = db.Set<ReferenceNode>();
        var navigation = ReferenceNavigation(efReference, efCollection);
        var source = mapped ? nodes : nodes.Where(Expression.Lambda<Func<ReferenceNode, bool>>(
            Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), new[] { typeof(ReferenceNode) }, navigation.Body), navigation.Parameters));
        Assert.Contains(mapped ? "ReferenceNodes" : "EXISTS", source.ToQueryString());
        var mapping = ReferenceMapping(mapped ? navigation : n => n.Children);
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        if (adapter)
        {
            await Assert.ThrowsAsync<Xunit.Sdk.XunitException>(() => source.ToProjectedListAsync(transformer, ProjectionDirective.All,
                mapping, keySource: explicitKeys ? nodes : null));
            Assert.Equal(1, noIo.OpenAttempts);
            return;
        }
        var executor = new ReferenceExecutor();
        var result = await transformer.RenderProjection(ProjectionDirective.All, mapping).ExecuteAsync(source, executor, keySource: explicitKeys ? nodes : null);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, executor.Commands.Count);
        Assert.Equal(2, result.Items[0].Children.Count);
        Assert.Equal(mapped ? 2 : 0, result.Items[1].Children.Count);
        Assert.Equal(0, noIo.OpenAttempts);
    }

    public static IEnumerable<object[]> ReferenceScopes()
    {
        foreach (var reference in new[] { false, true })
        foreach (var collection in new[] { false, true })
        foreach (var mapped in new[] { false, true })
        foreach (var adapter in new[] { false, true })
        foreach (var keys in new[] { false, true })
            yield return new object[] { reference, collection, mapped, adapter, keys };
    }

    private static Expression<Func<ReferenceNode, IEnumerable<ReferenceNode>>> ReferenceNavigation(bool reference, bool collection) =>
        reference ? collection ? n => EF.Property<List<ReferenceNode>>(EF.Property<ReferenceNode>(n, "Parent"), "Children")
            : n => EF.Property<ReferenceNode>(n, "Parent").Children
        : collection ? n => EF.Property<List<ReferenceNode>>(n.Parent!, "Children") : n => n.Parent!.Children;

    private static LinqProjectionMapping<ReferenceNode, ReferenceDto> ReferenceMapping(Expression<Func<ReferenceNode, IEnumerable<ReferenceNode>>> navigation) =>
        new LinqProjectionMapping<ReferenceNode, ReferenceDto>().Key("id", n => n.Id).Field("name", "name", n => n.Name, n => n.Name)
            .Collection("children", "children", navigation, n => n.Children,
                new LinqProjectionMapping<ReferenceNode, ReferenceDto>().Key("id", n => n.Id).Field("name", "name", n => n.Name, n => n.Name),
                new LinqQueryMapping<ReferenceNode>());

    [Fact]
    public void ReferenceMetadata_RejectsUnmappedNamesAndApplicationOwners()
    {
        using var db = new ReferenceContext(new NoIo());
        var nodes = db.Set<ReferenceNode>();
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(nodes.Where(n => EF.Property<ReferenceNode>(n, "Missing").Children.Any())));
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(nodes.Where(n => n.HiddenParent.Children.Any())));
        var name = "Parent";
        transformer.ValidateSource(nodes.Where(n => EF.Property<ReferenceNode>(n, name).Children.Any()));
    }

    private sealed class ReferenceContext : DbContext
    {
        internal ReferenceContext(NoIo noIo) : base(new DbContextOptionsBuilder<ReferenceContext>().UseSqlServer("Server=unused;Database=unused")
            .AddInterceptors(noIo).Options) { }
        protected override void OnModelCreating(ModelBuilder model) => model.Entity<ReferenceNode>().ToTable("ReferenceNodes").Ignore(n => n.HiddenParent)
            .HasMany(n => n.Children).WithOne(n => n.Parent).HasForeignKey(n => n.ParentId);
    }

    private sealed class ReferenceNode
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int? ParentId { get; set; }
        public ReferenceNode? Parent { get; set; }
        public List<ReferenceNode> Children { get; set; } = new();
        public ReferenceNode HiddenParent => throw new Xunit.Sdk.XunitException("Do not invoke application getters.");
    }
    private sealed class ReferenceDto
    {
        public ReferenceDto() { }
        public string Name { get; set; } = "";
        public List<ReferenceDto> Children { get; set; } = new();
    }
    private sealed class ReferenceExecutor : IProjectionQueryExecutor
    {
        internal readonly List<string> Commands = new();
        public Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            Commands.Add(query.ToQueryString());
            var parent = new ReferenceNode { Id = 1, Name = "parent", ParentId = 1 };
            var child = new ReferenceNode { Id = 2, Name = "child", ParentId = 1, Parent = parent };
            parent.Parent = parent;
            parent.Children.AddRange(new[] { parent, child });
            var data = new[] { parent, child }.AsQueryable();
            var expression = new ReferenceData(data).Visit(query.Expression)!;
            return Task.FromResult<IReadOnlyList<T>>(data.Provider.CreateQuery<T>(expression).ToList());
        }
    }
    private sealed class ReferenceData : ExpressionVisitor
    {
        private readonly IQueryable<ReferenceNode> _data;
        internal ReferenceData(IQueryable<ReferenceNode> data) => _data = data;
        protected override Expression VisitExtension(Expression node) => node is EntityQueryRootExpression ? _data.Expression : base.VisitExtension(node);
        protected override Expression VisitMethodCall(MethodCallExpression node) => node.Method.DeclaringType == typeof(EF)
            ? Expression.Property(Visit(node.Arguments[0]), (string)((ConstantExpression)node.Arguments[1]).Value!) : base.VisitMethodCall(node);
    }
}
