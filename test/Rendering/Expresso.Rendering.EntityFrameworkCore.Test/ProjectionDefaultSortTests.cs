using Expresso.Core.Projection;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed class ProjectionDefaultSortTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GrandchildSort_KeepsLocalDefaultUnlessExplicitlyOverridden(bool explicitParent)
    {
        // Scenario: actual provider SQL at each level plus graph execution; the child's order is independent of the leaf override.
        using var db = new ProjectionContext();
        var sql = new List<string>();
        var executor = new ProjectionSqlExecutor<ProjectionParent>(OrderedProjectionFixture.Data(), q => sql.Add(q.ToQueryString()));
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var result = await transformer.RenderProjection(ProjectionDirective.All, OrderedProjectionFixture.Mapping())
            .ExecuteAsync(db.Parents.Where(p => p.Tenant == 1 && p.Id == 1), executor, transformer, OrderedProjectionFixture.Sort(explicitParent));
        var parent = Assert.Single(result.Items);
        Assert.Equal(explicitParent ? new[] { "beta", "alpha" } : new[] { "alpha", "beta" }, parent.Children.Select(c => c.Name));
        Assert.Equal(new[] { "zulu", "first" }, parent.Children.Single(c => c.Name == "beta").Leaves.Select(l => l.Value));
        Assert.Contains("ORDER BY", sql[1]);
        Assert.Contains("ORDER BY", sql[2]);
        var localOrder = sql[1].Substring(sql[1].LastIndexOf("ORDER BY", StringComparison.Ordinal)).Trim();
        Assert.Equal(explicitParent, localOrder.EndsWith("DESC", StringComparison.Ordinal));
    }
}
