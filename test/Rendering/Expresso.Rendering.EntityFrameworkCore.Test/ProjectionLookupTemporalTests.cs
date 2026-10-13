using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed partial class ProjectionTemporalTests
{
    [Theory]
    [MemberData(nameof(LookupScopes))]
    public async Task QueryValuedLookups_RejectBeforeAnyRead(string shape, string keys, bool adapter)
    {
        // Scenario: a BCL container conceals a historical query; never evaluate its element operator before rejection.
        var noIo = new NoIo();
        using var db = new TemporalContext(noIo);
        var source = LookupScope(db, shape);
        Assert.Contains("FOR SYSTEM_TIME", source.ToQueryString());
        var current = db.Set<ProjectionParent>();
        var root = keys == "only" ? current : source;
        var keySource = keys == "inferred" ? null : source;
        var executor = new ProjectionSqlExecutor(_ => throw new Xunit.Sdk.XunitException("Unexpected read."));
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        await Assert.ThrowsAsync<NotSupportedException>(() => adapter
            ? root.ToProjectedListAsync(transformer, ProjectionFixture.Nested(), ProjectionFixture.Mapping(), keySource: keySource)
            : transformer.RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping()).ExecuteAsync(root, executor, keySource: keySource));
        Assert.Empty(executor.RowCounts);
        Assert.Equal(0, noIo.OpenAttempts);
    }

    public static IEnumerable<object[]> LookupScopes()
    {
        foreach (var shape in new[] { "first", "enumerable", "single", "element", "default", "queryable" })
        foreach (var keys in new[] { "inferred", "explicit", "only" })
        foreach (var adapter in new[] { false, true })
            yield return new object[] { shape, keys, adapter };
    }

    private static IQueryable<ProjectionParent> LookupScope(TemporalContext db, string shape)
    {
        var history = Temporal(db, "AsOf");
        var queries = new[] { history };
        var enumerable = new IEnumerable<ProjectionParent>[] { history };
        var list = queries.ToList();
        var queryable = queries.AsQueryable();
        var anchor = db.Set<ProjectionParent>().Where(p => p.Tenant == 1 && p.Id == 1);
        return shape switch
        {
            "enumerable" => anchor.SelectMany(_ => enumerable.First()),
            "single" => anchor.SelectMany(_ => list.Single()),
            "element" => anchor.SelectMany(_ => queries.ElementAt(0)),
            "default" => anchor.SelectMany(_ => queries.FirstOrDefault()!),
            "queryable" => anchor.SelectMany(_ => queryable.First()),
            _ => anchor.SelectMany(_ => queries.First())
        };
    }

    [Fact]
    public void ScalarElementOperators_RemainSupported()
    {
        using var db = new TemporalContext();
        var ids = new[] { 1, 2 };
        var source = db.Set<ProjectionParent>().Where(p => p.Id == ids.First() && p.Id == ids.ElementAt(0));
        new EfCoreExpressionToLinqTransformer(db.Database.ProviderName).ValidateSource(source);
        Assert.DoesNotContain("FOR SYSTEM_TIME", source.ToQueryString());
    }

    [Fact]
    public void UnseenSequenceRows_AreNotTrusted_ButDefinedCollectionRowsAndGroupsRemainSupported()
    {
        // Scenario: composing a flattened captured query container must not bypass lookup rejection; proven query rows remain safe.
        using var db = new TemporalContext();
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        IEnumerable<ProjectionParent>[] queries = { Temporal(db, "AsOf") };
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(db.Set<ProjectionParent>()
            .SelectMany(_ => queries.SelectMany(query => query))));
        var defined = db.Set<ProjectionParent>().Select(p => p.Children).SelectMany(children => children);
        transformer.ValidateSource(defined);
        Assert.Contains("JOIN [Children]", defined.ToQueryString());
        var grouped = db.Set<ProjectionParent>().GroupBy(p => p.Tenant).Select(group => group.OrderBy(p => p.Id).First());
        transformer.ValidateSource(grouped);
        Assert.Contains("ROW_NUMBER", grouped.ToQueryString());
    }
}
