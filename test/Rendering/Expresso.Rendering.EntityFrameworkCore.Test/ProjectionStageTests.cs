using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed partial class ProjectionTemporalTests
{
    [Theory]
    [MemberData(nameof(StageScopes))]
    public async Task InspectableStages_RetainSourceDefinitions(string shape, bool adapter, bool explicitKeys)
    {
        // Scenario: generators, wrappers and both conditional row branches remain translatable; preserve root/child graph semantics.
        var noIo = new NoIo();
        using var db = new TemporalContext(noIo);
        var parents = db.Set<ProjectionParent>();
        var source = Stage(parents, shape);
        Assert.DoesNotContain("FOR SYSTEM_TIME", source.Select(p => p.Title).ToQueryString());
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        if (adapter)
        {
            await Assert.ThrowsAsync<Xunit.Sdk.XunitException>(() => source.ToProjectedListAsync(transformer,
                ProjectionFixture.Nested(), ProjectionFixture.Mapping(), keySource: explicitKeys ? parents : null));
            Assert.Equal(1, noIo.OpenAttempts);
            return;
        }
        var commands = new List<string>();
        var result = await transformer.RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping())
            .ExecuteAsync(source, new StageExecutor(commands), keySource: explicitKeys ? parents : null);
        var empty = shape == "conditional-false";
        var generator = shape is "empty-any" or "empty-count" or "range" or "repeat";
        Assert.Equal(empty ? 0 : generator ? 3 : 2, result.Items.Count);
        Assert.Equal(empty ? 1 : 3, commands.Count);
        if (!empty) Assert.Equal("first", result.Items[0].Children[0].Leaves[0].Value);
        Assert.Equal(0, noIo.OpenAttempts);
    }

    public static IEnumerable<object[]> StageScopes()
    {
        foreach (var shape in new[] { "empty-any", "empty-count", "range", "repeat", "conditional-true", "conditional-false",
            "TagWith", "TagWithCallSite", "AsTracking", "AsNoTracking", "IdentityResolution", "IgnoreAutoIncludes", "IgnoreQueryFilters", "AsSplitQuery", "AsSingleQuery" })
        foreach (var adapter in new[] { false, true })
        foreach (var explicitKeys in new[] { false, true })
            yield return new object[] { shape, adapter, explicitKeys };
    }

    private static IQueryable<ProjectionParent> Stage(IQueryable<ProjectionParent> parents, string shape)
    {
        if (shape == "empty-any") return parents.Where(p => !Enumerable.Empty<int>().Any());
        if (shape == "empty-count") return parents.Where(p => p.Id > Enumerable.Empty<int>().Count());
        if (shape == "range") return parents.Where(p => Enumerable.Range(1, 2).Contains(p.Id));
        if (shape == "repeat") return parents.Where(p => Enumerable.Repeat(1, 2).Contains(p.Tenant) || p.Tenant == 2);
        if (shape.StartsWith("conditional", StringComparison.Ordinal))
        {
            var positive = shape == "conditional-true";
            return parents.Select(p => positive
                    ? new { Parent = p, Kids = p.Children.Where(c => c.Id > 0) }
                    : new { Parent = p, Kids = p.Children.Where(c => c.Id < 0) })
                .Where(x => x.Kids.Any()).Select(x => x.Parent);
        }
        return Wrap(parents.Select(p => new { Parent = p, Kids = p.Children }), shape)
            .Where(x => x.Kids.Any()).Select(x => x.Parent);
    }

    private static IQueryable<T> Wrap<T>(IQueryable<T> source, string shape) where T : class => shape switch
    {
        "TagWith" => source.TagWith("projection stage"),
        "TagWithCallSite" => source.TagWithCallSite(),
        "AsTracking" => source.AsTracking(),
        "AsNoTracking" => source.AsNoTracking(),
        "IdentityResolution" => source.AsNoTrackingWithIdentityResolution(),
        "IgnoreAutoIncludes" => source.IgnoreAutoIncludes(),
        "IgnoreQueryFilters" => source.IgnoreQueryFilters(),
        "AsSplitQuery" => source.AsSplitQuery(),
        _ => source.AsSingleQuery()
    };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConditionalStages_InspectInactiveUnsafeBranches(bool chooseCurrent)
    {
        // Scenario: trace both row definitions, never only the currently selected one.
        using var db = new TemporalContext();
        var history = Temporal(db, "AsOf");
        var source = db.Set<ProjectionParent>().Select(p => chooseCurrent
            ? new { Parent = p, Kids = p.Children.AsEnumerable() }
            : new { Parent = p, Kids = history.SelectMany(x => x.Children).AsEnumerable() }).TagWith("conditional")
            .Where(x => x.Kids.Any()).Select(x => x.Parent);
        Assert.Throws<NotSupportedException>(() => new EfCoreExpressionToLinqTransformer(db.Database.ProviderName).ValidateSource(source));
    }
}
