using Expresso.Core.Projection;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.Linq.Test;

public sealed class ProjectionSecondReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GrandchildSort_PreservesDefaultOrExplicitParentOrder(bool explicitParent)
    {
        // Scenario: only leaves are sorted unless a local override is supplied; the intermediate default remains effective.
        var transformer = new QueryableExpressionToLinqTransformer();
        var result = await transformer.RenderProjection(ProjectionDirective.All, OrderedProjectionFixture.Mapping())
            .ExecuteAsync(OrderedProjectionFixture.Data().Where(p => p.Tenant == 1 && p.Id == 1), new MemoryProjectionExecutor(),
                transformer, OrderedProjectionFixture.Sort(explicitParent));
        var parent = Assert.Single(result.Items);
        Assert.Equal(explicitParent ? new[] { "beta", "alpha" } : new[] { "alpha", "beta" }, parent.Children.Select(c => c.Name));
        Assert.Equal(new[] { "zulu", "first" }, parent.Children.Single(c => c.Name == "beta").Leaves.Select(l => l.Value));
    }
}
