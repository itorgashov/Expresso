using Expresso.Core.Projection;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.Linq.Test;

public sealed class ProjectionReviewTests
{
    [Fact]
    public async Task BinaryCompositeKeys_MatchByContent_WithoutCrossAttachment()
    {
        // Scenario: copied binary identities share an integer component; null identities must not match any batch.
        var data = new[] {
            new BinaryParent { Tenant = new byte[] { 1, 2 }, Id = 1, Children = ProjectionFixture.Data()[0].Children },
            new BinaryParent { Tenant = new byte[] { 3 }, Id = 1, Children = ProjectionFixture.Data()[1].Children },
            new BinaryParent { Tenant = null, Id = 1, Children = ProjectionFixture.Data()[0].Children } };
        var map = new LinqProjectionMapping<BinaryParent, ProjectionParentDto>()
            .Key("tenant", x => x.Tenant).Key("id", x => x.Id)
            .Collection("children", "children", x => x.Children, x => x.Children, Child(), Query());
        var result = await new InMemoryExpressionToLinqTransformer().RenderProjection(ProjectionDirective.All, map)
            .ExecuteAsync(data.AsQueryable(), new MemoryProjectionExecutor(), options: new ProjectionExecutionOptions { MaxKeyParameters = 2 });
        Assert.Equal(new[] { "beta", "alpha" }, result.Items[0].Children.Select(x => x.Name));
        Assert.Equal("gamma", Assert.Single(result.Items[1].Children).Name);
        Assert.Empty(result.Items[2].Children);
        Assert.DoesNotContain("tenant", ProjectionShaper.ToSparseObjects(result)[0].Keys);
    }

    [Fact]
    public async Task SelectedCollections_DiscardConstructorDefaults_EvenWhenNoChildrenMatch()
    {
        // Scenario: DTO constructors seed collections; returned collections must contain only actual query rows.
        var map = new LinqProjectionMapping<ProjectionParent, SeededDto>().Key("tenant", x => x.Tenant).Key("id", x => x.Id)
            .Collection("children", "children", x => x.Children, x => x.Children, Child(), Query());
        var result = await new QueryableExpressionToLinqTransformer().RenderProjection(ProjectionDirective.All, map)
            .ExecuteAsync(ProjectionFixture.Data().AsQueryable(), new MemoryProjectionExecutor());
        Assert.Equal(new[] { "beta", "alpha" }, result.Items[0].Children.Select(x => x.Name));
        Assert.Empty(result.Items[2].Children);
        var incompleteMap = new LinqProjectionMapping<BinaryParent, SeededDto>().Key("tenant", x => x.Tenant)
            .Collection("children", "children", x => x.Children, x => x.Children, Child(), Query());
        var incomplete = await new QueryableExpressionToLinqTransformer().RenderProjection(ProjectionDirective.All, incompleteMap)
            .ExecuteAsync(new[] { new BinaryParent() }.AsQueryable(), new MemoryProjectionExecutor());
        Assert.Empty(incomplete.Items[0].Children);
    }

    [Fact]
    public void KeySelectors_MustReferToTheEntity_NotCapturedOrStaticMembers()
    {
        // Scenario: captured/static members produce a constant identity and merge unrelated parents.
        var captured = ProjectionFixture.Data()[0];
        Assert.Throws<ArgumentException>(() => new LinqProjectionMapping<ProjectionParent, ProjectionParentDto>().Key("id", x => captured.Id));
        Assert.Throws<ArgumentException>(() => new LinqProjectionMapping<ProjectionParent, ProjectionParentDto>().Key("id", x => StaticIdentity));
    }

    [Fact]
    public async Task UnselectedCollections_KeepDtoDefaults_ButRemainAbsentFromTheResponse()
    {
        var map = new LinqProjectionMapping<ProjectionParent, SeededDto>().Key("id", x => x.Id)
            .Field("title", "title", x => x.Title, x => x.Title)
            .Collection("children", "children", x => x.Children, x => x.Children, Child(), Query());
        var executor = new MemoryProjectionExecutor();
        var result = await new QueryableExpressionToLinqTransformer().RenderProjection(
            new ProjectionDirective(new[] { new ProjectionSelection("title") }), map)
            .ExecuteAsync(ProjectionFixture.Data().AsQueryable(), executor);
        Assert.Equal("not from database", Assert.Single(result.Items[0].Children).Name);
        Assert.Single(executor.Queries);
        Assert.Equal(new[] { "title" }, ProjectionShaper.ToSparseObjects(result)[0].Keys);
    }

    private static int StaticIdentity => 1;
    private static LinqProjectionMapping<ProjectionChild, ProjectionChildDto> Child() =>
        new LinqProjectionMapping<ProjectionChild, ProjectionChildDto>().Key("tenant", x => x.Tenant).Key("id", x => x.Id)
            .Field("name", "name", x => x.Name, x => x.Name);
    private static LinqQueryMapping<ProjectionChild> Query() => new LinqQueryMapping<ProjectionChild>().Field("name", x => x.Name);
    public sealed class BinaryParent
    {
        public byte[]? Tenant { get; set; }
        public int Id { get; set; }
        public List<ProjectionChild> Children { get; set; } = new List<ProjectionChild>();
    }
    public sealed class SeededDto
    {
        public string Title { get; set; } = "";
        public List<ProjectionChildDto> Children { get; set; } = new List<ProjectionChildDto> { new ProjectionChildDto { Name = "not from database" } };
    }
}
