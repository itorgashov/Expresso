using System.Linq.Expressions;
using Expresso.Core.Projection;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed partial class ProjectionTemporalTests
{
    [Theory]
    [MemberData(nameof(CapturedScopes))]
    public async Task CapturedTemporalCollections_AreRejectedBeforeAnyRead(string mode, bool explicitKeySource, bool adapter, string capture)
    {
        // Scenario: EF expands a stored historical query in SelectMany; both paths must reject before materialization/opening.
        var noIo = new NoIo();
        using var db = new TemporalContext(noIo);
        var source = Capture(db, Temporal(db, mode), capture);
        Assert.Contains("FOR SYSTEM_TIME", source.ToQueryString()); // Genuine translation of the unmodified source, no execution.
        var executor = new ProjectionSqlExecutor(_ => throw new Xunit.Sdk.XunitException("Unexpected read before rejection."));
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var projection = ProjectionFixture.Nested();
        var mapping = ProjectionFixture.Mapping();
        var error = await Assert.ThrowsAsync<NotSupportedException>(() => adapter
            ? source.ToProjectedListAsync(transformer, projection, mapping, keySource: explicitKeySource ? source : null)
            : transformer.RenderProjection(projection, mapping).ExecuteAsync(source, executor, keySource: explicitKeySource ? source : null));
        Assert.Contains("Temporal", error.Message);
        Assert.Empty(executor.RowCounts);
        Assert.Equal(0, noIo.OpenAttempts);
    }

    [Theory]
    [InlineData("field")]
    [InlineData("nested")]
    [InlineData("boxed")]
    public void CapturedCurrentCollections_RemainTranslatable(string capture)
    {
        // Scenario: the same captured-query composition without history remains accepted and provider-translatable.
        using var db = new TemporalContext();
        var current = Capture(db, db.Set<ProjectionParent>(), capture);
        new EfCoreExpressionToLinqTransformer(db.Database.ProviderName).ValidateSource(current);
        Assert.DoesNotContain("FOR SYSTEM_TIME", current.ToQueryString());
    }

    [Theory]
    [InlineData("field")]
    [InlineData("nested")]
    [InlineData("boxed")]
    public async Task CapturedScalarTemporalProjection_RetainsHistoricalSql(string capture)
    {
        // Scenario: scalar-only projection still translates the original captured historical source without graph validation.
        using var db = new TemporalContext();
        var plan = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName).RenderProjection(
            new ProjectionDirective(new[] { new ProjectionSelection("title") }), ProjectionFixture.Mapping());
        var executor = new TranslateOnlyExecutor();
        await plan.ExecuteAsync(Capture(db, Temporal(db, "AsOf"), capture), executor);
        Assert.Contains("FOR SYSTEM_TIME", Assert.Single(executor.Commands));
    }

    public static IEnumerable<object[]> CapturedScopes()
    {
        foreach (var scope in Scopes())
        foreach (var capture in new[] { "field", "nested", "boxed" })
            yield return scope.Concat(new object[] { capture }).ToArray();
    }

    private static IQueryable<ProjectionParent> Capture(TemporalContext db, IQueryable<ProjectionParent> historical, string capture)
    {
        var anchor = db.Set<ProjectionParent>().Where(p => p.Tenant == 1 && p.Id == 1);
        if (capture == "nested")
        {
            var holder = new CaptureHolder { Query = historical };
            return anchor.SelectMany(_ => holder.Query);
        }
        if (capture == "boxed")
        {
            object boxed = historical;
            return anchor.SelectMany(_ => (IQueryable<ProjectionParent>)boxed);
        }
        return anchor.SelectMany(_ => historical);
    }

    private sealed class CaptureHolder
    {
        public IQueryable<ProjectionParent> Query = null!;
    }

    private sealed class TranslateOnlyExecutor : IProjectionQueryExecutor
    {
        internal List<string> Commands { get; } = new();
        public Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            Commands.Add(query.ToQueryString());
            return Task.FromResult<IReadOnlyList<T>>(Array.Empty<T>());
        }
    }
}
