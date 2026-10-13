using Expresso.Core.Projection;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed partial class ProjectionTemporalTests
{
    [Theory]
    [MemberData(nameof(EnumerableProducerScopes))]
    public async Task EnumerableProducers_RejectBeforeApplicationEvaluationOrRead(string shape, string keyMode, bool adapter)
    {
        // Scenario: seven enumerable-typed producers expand to history in EF; neither validation path may invoke them or read.
        var noIo = new NoIo();
        using var db = new TemporalContext(noIo);
        var holder = new EnumerableQueryHolder(Temporal(db, "AsOf"));
        var scope = EnumerableScope(db, holder, shape);
        Assert.Contains("FOR SYSTEM_TIME", scope.ToQueryString());
        holder.Calls = 0; // Translation above is separate evidence; validation itself must not evaluate the producer.
        var source = keyMode == "keys-only" ? db.Set<ProjectionParent>() : scope;
        var keys = keyMode == "inferred" ? null : scope;
        var executor = new ProjectionSqlExecutor(_ => throw new Xunit.Sdk.XunitException("Unexpected materialization."));
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        await Assert.ThrowsAsync<NotSupportedException>(() => adapter
            ? source.ToProjectedListAsync(transformer, ProjectionFixture.Nested(), ProjectionFixture.Mapping(), keySource: keys)
            : transformer.RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping()).ExecuteAsync(source, executor, keySource: keys));
        Assert.Equal(0, holder.Calls);
        Assert.Empty(executor.RowCounts);
        Assert.Equal(0, noIo.OpenAttempts);
    }

    [Fact]
    public async Task EnumerableProducer_ScalarOnlyHistoryStillTranslates()
    {
        // Scenario: without selected collections the scalar query remains host/provider-owned and retains historical SQL.
        using var db = new TemporalContext();
        var source = EnumerableScope(db, new EnumerableQueryHolder(Temporal(db, "AsOf")), "property");
        var executor = new TranslateOnlyExecutor();
        await new EfCoreExpressionToLinqTransformer(db.Database.ProviderName)
            .RenderProjection(new ProjectionDirective(new[] { new ProjectionSelection("title") }), ProjectionFixture.Mapping())
            .ExecuteAsync(source, executor);
        Assert.Contains("FOR SYSTEM_TIME", Assert.Single(executor.Commands));
    }

    public static IEnumerable<object[]> EnumerableProducerScopes()
    {
        foreach (var shape in new[] { "property", "method", "delegate", "array", "indexer", "as-queryable", "boxed" })
        foreach (var keys in new[] { "inferred", "explicit", "keys-only" })
        foreach (var adapter in new[] { false, true })
            yield return new object[] { shape, keys, adapter };
    }

    private static IQueryable<ProjectionParent> EnumerableScope(TemporalContext db, EnumerableQueryHolder holder, string shape)
    {
        var anchor = db.Set<ProjectionParent>().Where(p => p.Tenant == 1 && p.Id == 1);
        Func<IEnumerable<ProjectionParent>> factory = holder.Create;
        var array = new[] { holder.Stored };
        var list = array.ToList();
        return shape switch
        {
            "property" => anchor.SelectMany(_ => holder.Query),
            "method" => anchor.SelectMany(_ => holder.Create()),
            "delegate" => anchor.SelectMany(_ => factory()),
            "array" => anchor.SelectMany(_ => array[0]),
            "indexer" => anchor.SelectMany(_ => list[0]),
            "as-queryable" => anchor.SelectMany(_ => holder.Query.AsQueryable()),
            _ => anchor.SelectMany(_ => (IEnumerable<ProjectionParent>)holder.Boxed)
        };
    }

    private sealed class EnumerableQueryHolder
    {
        internal EnumerableQueryHolder(IQueryable<ProjectionParent> query) { Stored = query; }
        public readonly IEnumerable<ProjectionParent> Stored;
        internal int Calls;
        public IEnumerable<ProjectionParent> Query => Create();
        public object Boxed => Create();
        public IEnumerable<ProjectionParent> Create() { Calls++; return Stored; }
    }
}
