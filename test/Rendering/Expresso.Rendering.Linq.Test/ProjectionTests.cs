using System.Linq.Expressions;
using Expresso.Core.Projection;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Sorting;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.Linq.Test;

public sealed class ProjectionTests
{
    [Fact]
    public async Task PagedRootsAndDeepBatches_OnlyLoadThePage_AndRespectEachQueryBudget()
    {
        // Scenario: root filter/order/page use hidden fields; descendant batches must not accumulate ancestor parameters.
        var source = ProjectionFixture.Data().AsQueryable().Where(p => p.Tenant == 1).OrderBy(p => p.Title).Skip(1).Take(1);
        var directive = new ProjectionDirective(new[] { new ProjectionSelection("children", new[] {
            new ProjectionSelection("leaves", new[] { new ProjectionSelection("value") }) }) });
        var executor = new MemoryProjectionExecutor();
        var result = await new QueryableExpressionToLinqTransformer().RenderProjection(directive, ProjectionFixture.Mapping())
            .ExecuteAsync(source, executor, options: new ProjectionExecutionOptions { MaxKeyParameters = 2 });
        var root = Assert.Single(ProjectionShaper.ToSparseObjects(result));
        Assert.Equal(new[] { "children" }, root.Keys);
        Assert.Equal(2, result.Items[0].Children.Count);
        Assert.Equal("first", result.Items[0].Children[0].Leaves[0].Value);
        Assert.All(executor.Expressions, expression =>
        {
            var counter = new KeyParameterCounter();
            counter.Visit(expression);
            Assert.InRange(counter.Count, 0, 2);
        });
    }

    [Fact]
    public async Task CompositeKeys_NestedCollectionsAndBatches_DoNotCrossAttach()
    {
        // Scenario: parents and children repeat Id in two tenants; one key per batch must retain every component.
        var map = ProjectionFixture.Mapping();
        var executor = new MemoryProjectionExecutor();
        var result = await new QueryableExpressionToLinqTransformer().RenderProjection(ProjectionFixture.Nested(), map)
            .ExecuteAsync(ProjectionFixture.Data().AsQueryable(), executor, options: new ProjectionExecutionOptions { MaxKeyParameters = 2 });
        Assert.Equal("first", result.Items[0].Children[0].Leaves[0].Value);
        Assert.Equal("second", result.Items[1].Children[0].Leaves[0].Value);
        Assert.Empty(result.Items[2].Children);
        Assert.Equal(7, executor.Queries.Count); // Root; 3 child batches; 3 leaf batches, including an empty result.
        var shape = ProjectionShaper.ToSparseObjects(result);
        Assert.Equal(new[] { "title", "children" }, shape[0].Keys);
        Assert.DoesNotContain("id", shape[0].Keys);
        Assert.Equal(0, result.Items[0].Id);
    }

    [Fact]
    public async Task ScalarsOnly_OmitChildren_AndPreserveSelectedNull()
    {
        var map = ProjectionFixture.Mapping();
        var projection = new ProjectionDirective(new[] { new ProjectionSelection("notes") });
        var plan = new InMemoryExpressionToLinqTransformer().RenderProjection(projection, map);
        Assert.Contains("Notes =", plan.Selector.ToString());
        Assert.DoesNotContain("Title =", plan.Selector.ToString());
        var executor = new MemoryProjectionExecutor();
        var result = await plan.ExecuteAsync(ProjectionFixture.Data().AsQueryable(), executor);
        Assert.Single(executor.Queries);
        Assert.Null(ProjectionShaper.ToSparseObjects(result)[0]["notes"]);
        Assert.Single(ProjectionShaper.ToSparseObjects(result)[0]);
        Assert.Throws<InvalidOperationException>(() => map.Key("extra", x => x.Id));
    }

    [Fact]
    public async Task AllAndBareCollections_LoadDefaultSubtree()
    {
        foreach (var directive in new[] { ProjectionDirective.All,
            new ProjectionDirective(new[] { new ProjectionSelection("children", selectDefaultSubtree: true) }) })
        {
            var result = await new QueryableExpressionToLinqTransformer().RenderProjection(directive, ProjectionFixture.Mapping())
                .ExecuteAsync(ProjectionFixture.Data().AsQueryable(), new MemoryProjectionExecutor());
            Assert.Single(result.Items[0].Children[0].Leaves);
            Assert.Equal("beta", result.Items[0].Children[0].Name);
        }
    }

    [Fact]
    public async Task NestedSort_UsesNonReturnedFields()
    {
        var sort = new SortDirective(Array.Empty<SortDirectiveItem>(), new[] { new CollectionSort("children",
            new SortDirective(new[] { new SortDirectiveItem { Expression = new Field("name", typeof(string)), Direction = SortDirection.Ascending } })) });
        var directive = new ProjectionDirective(new[] { new ProjectionSelection("children", new[] {
            new ProjectionSelection("leaves", selectDefaultSubtree: true) }) });
        var plan = new QueryableExpressionToLinqTransformer().RenderProjection(directive, ProjectionFixture.Mapping());
        var result = await plan.ExecuteAsync(ProjectionFixture.Data().AsQueryable(), new MemoryProjectionExecutor(),
            new QueryableExpressionToLinqTransformer(), sort);
        Assert.Empty(result.Items[0].Children[0].Leaves); // alpha precedes beta, though name is not returned.
        Assert.Single(result.Items[0].Children[1].Leaves);
        Assert.DoesNotContain("name", ((IReadOnlyDictionary<string, object?>[])ProjectionShaper.ToSparseObjects(result)[0]["children"]!)[0].Keys);
    }

    [Fact]
    public async Task EmptyRoots_AndCancellation_DoNotRunChildren()
    {
        var executor = new MemoryProjectionExecutor();
        var plan = new QueryableExpressionToLinqTransformer().RenderProjection(ProjectionDirective.All, ProjectionFixture.Mapping());
        Assert.Empty((await plan.ExecuteAsync(Array.Empty<ProjectionParent>().AsQueryable(), executor)).Items);
        Assert.Single(executor.Queries);
        await Assert.ThrowsAsync<OperationCanceledException>(() => plan.ExecuteAsync(ProjectionFixture.Data().AsQueryable(), executor,
            cancellationToken: new CancellationToken(true)));
        await Assert.ThrowsAsync<ArgumentNullException>(() => plan.ExecuteAsync(null!, executor));
        await Assert.ThrowsAsync<ArgumentNullException>(() => plan.ExecuteAsync(ProjectionFixture.Data().AsQueryable(), null!));
        await Assert.ThrowsAsync<ArgumentException>(() => plan.ExecuteAsync(ProjectionFixture.Data().AsQueryable(), executor,
            options: new ProjectionExecutionOptions { MaxKeyParameters = 1 }));
    }

    [Fact]
    public void InvalidSelectionsAndMappings_FailBeforeExecution()
    {
        var transformer = new QueryableExpressionToLinqTransformer();
        Assert.Throws<ArgumentNullException>(() => transformer.RenderProjection<ProjectionParent, ProjectionParentDto>(null!, ProjectionFixture.Mapping()));
        Assert.Throws<ArgumentNullException>(() => transformer.RenderProjection<ProjectionParent, ProjectionParentDto>(ProjectionDirective.All, null!));
        Assert.Throws<ArgumentException>(() => transformer.RenderProjection(ProjectionDirective.All, new LinqProjectionMapping<ProjectionParent, ProjectionParentDto>()));
        Assert.Throws<ArgumentException>(() => transformer.RenderProjection(new ProjectionDirective(new[] { new ProjectionSelection("missing") }), ProjectionFixture.Mapping()));
        Assert.Throws<ArgumentException>(() => transformer.RenderProjection(new ProjectionDirective(new[] {
            new ProjectionSelection("title", new[] { new ProjectionSelection("x") }) }), ProjectionFixture.Mapping()));
        Assert.Throws<ArgumentException>(() => new LinqProjectionMapping<ProjectionParent, ProjectionParentDto>().Key("", x => x.Id));
        Assert.Throws<ArgumentException>(() => new LinqProjectionMapping<ProjectionParent, ProjectionParentDto>().Key("id", x => x.Id).Key("ID", x => x.Tenant));
        Assert.Throws<ArgumentException>(() => new LinqProjectionMapping<ProjectionParent, ProjectionParentDto>().Key("computed", x => x.Id + 1));
        Assert.Throws<ArgumentNullException>(() => new LinqProjectionMapping<ProjectionParent, ProjectionParentDto>().Key<int>("id", null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProjectionExecutionOptions { MaxKeyParameters = 0 });
        Assert.Throws<ArgumentNullException>(() => ProjectionShaper.ToSparseObjects<ProjectionParentDto>(null!));
        Assert.Equal(3, ProjectionFixture.Mapping().GetProjectionModel().Fields.Length);
    }

    private sealed class KeyParameterCounter : ExpressionVisitor
    {
        public int Count { get; private set; }
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is ConstantExpression capture && capture.Value?.GetType().Name == "ParameterBox`1") Count++;
            return base.VisitMember(node);
        }
    }
}
