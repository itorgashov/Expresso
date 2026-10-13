using Expresso.Core.Projection;
using System.Linq.Expressions;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed partial class ProjectionTemporalTests
{
    [Theory]
    [MemberData(nameof(MappedTemporalScopes))]
    public async Task SelectedMappedHistory_IsRejectedAtEveryDepthBeforeRootRead(string location, bool adapter, bool explicitKeys)
    {
        // Scenario: current root/key queries introduce history only through a selected mapping; preflight must reject before root I/O.
        var noIo = new NoIo();
        using var db = new TemporalContext(noIo);
        var children = db.Set<ProjectionChild>().TemporalAsOf(new DateTime(2020, 1, 1));
        var leaves = db.Set<ProjectionLeaf>().TemporalAsOf(new DateTime(2020, 1, 1));
        Assert.Contains("FOR SYSTEM_TIME", children.ToQueryString());
        Assert.Contains("FOR SYSTEM_TIME", leaves.ToQueryString());
        var mapping = MappedHistory(children, leaves, location);
        var executor = new ProjectionSqlExecutor(_ => throw new Xunit.Sdk.XunitException("Unexpected root materialization."));
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        IQueryable<ProjectionParent> source = db.Set<ProjectionParent>();
        await Assert.ThrowsAsync<NotSupportedException>(() => adapter
            ? source.ToProjectedListAsync(transformer, ProjectionDirective.All, mapping, keySource: explicitKeys ? source : null)
            : transformer.RenderProjection(ProjectionDirective.All, mapping).ExecuteAsync(source, executor, keySource: explicitKeys ? source : null));
        Assert.Empty(executor.RowCounts);
        Assert.Equal(0, noIo.OpenAttempts);
    }

    [Theory]
    [InlineData("children-leaf")]
    [InlineData("children-deep")]
    [InlineData("leaves")]
    public async Task UnselectedMappedHistory_DoesNotAffectCurrentProjection(string location)
    {
        // Scenario: history exists in an unselected mapping; only the requested current root/intermediate scope is inspected/executed.
        using var db = new TemporalContext();
        var mapping = MappedHistory(db.Set<ProjectionChild>().TemporalAsOf(new DateTime(2020, 1, 1)),
            db.Set<ProjectionLeaf>().TemporalAsOf(new DateTime(2020, 1, 1)), location);
        var selection = location == "leaves"
            ? new ProjectionDirective(new[] { ProjectionSelection.Subtree("children", new ProjectionDirective(new[] { new ProjectionSelection("name") })) })
            : new ProjectionDirective(new[] { new ProjectionSelection("title") });
        var commands = new List<string>();
        await new EfCoreExpressionToLinqTransformer(db.Database.ProviderName).RenderProjection(selection, mapping)
            .ExecuteAsync(db.Set<ProjectionParent>(), new ProjectionSqlExecutor(q => commands.Add(q.ToQueryString())));
        Assert.Equal(location == "leaves" ? 2 : 1, commands.Count);
        Assert.All(commands, sql => Assert.DoesNotContain("FOR SYSTEM_TIME", sql));
    }

    public static IEnumerable<object[]> MappedTemporalScopes()
    {
        foreach (var location in new[] { "children-leaf", "children-deep", "leaves" })
        foreach (var adapter in new[] { false, true })
        foreach (var explicitKeys in new[] { false, true })
            yield return new object[] { location, adapter, explicitKeys };
    }

    private static LinqProjectionMapping<ProjectionParent, ProjectionParentDto> MappedHistory(
        IQueryable<ProjectionChild> historicalChildren, IQueryable<ProjectionLeaf> historicalLeaves, string location)
    {
        var leaf = new LinqProjectionMapping<ProjectionLeaf, ProjectionLeafDto>().Key("tenant", l => l.Tenant).Key("id", l => l.Id)
            .Field("value", "value", l => l.Value, l => l.Value);
        var child = new LinqProjectionMapping<ProjectionChild, ProjectionChildDto>().Key("tenant", c => c.Tenant).Key("id", c => c.Id)
            .Field("name", "name", c => c.Name, c => c.Name);
        if (location != "children-leaf")
        {
            Expression<Func<ProjectionChild, IEnumerable<ProjectionLeaf>>> navigation = c => c.Leaves;
            if (location == "leaves") navigation = c => historicalLeaves.Where(l => l.Tenant == c.Tenant && l.ChildId == c.Id);
            child.Collection("leaves", "leaves", navigation, c => c.Leaves, leaf, new LinqQueryMapping<ProjectionLeaf>());
        }
        Expression<Func<ProjectionParent, IEnumerable<ProjectionChild>>> children = p => p.Children;
        if (location != "leaves") children = p => historicalChildren.Where(c => c.Tenant == p.Tenant && c.ParentId == p.Id);
        return new LinqProjectionMapping<ProjectionParent, ProjectionParentDto>().Key("tenant", p => p.Tenant).Key("id", p => p.Id)
            .Field("title", "title", p => p.Title, p => p.Title)
            .Collection("children", "children", children, p => p.Children, child, new LinqQueryMapping<ProjectionChild>());
    }
}
