using Expresso.Core.Projection;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed partial class ProjectionTemporalTests
{
    [Theory]
    [MemberData(nameof(Scopes))]
    public async Task TemporalCollections_AreRejectedBeforeAnyRead(string mode, bool explicitKeySource, bool adapter)
    {
        // Scenario: a historical graph must never mix current children; reject both entry points before SQL/materialization.
        var noIo = new NoIo();
        using var db = new TemporalContext(noIo);
        var scope = Temporal(db, mode).Where(p => p.Tenant == 1);
        var executor = new ProjectionSqlExecutor(_ => throw new Xunit.Sdk.XunitException("Unexpected SQL/read before rejection."));
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var projection = ProjectionFixture.Nested();
        var mapping = ProjectionFixture.Mapping();
        var page = scope.OrderBy(p => p.Id).Take(1);
        var error = await Assert.ThrowsAsync<NotSupportedException>(() => adapter
            ? page.ToProjectedListAsync(transformer, projection, mapping, keySource: explicitKeySource ? scope : null)
            : transformer.RenderProjection(projection, mapping).ExecuteAsync(page, executor, keySource: explicitKeySource ? scope : null));
        Assert.Contains("Temporal", error.Message);
        Assert.Empty(executor.RowCounts);
        Assert.Equal(0, noIo.OpenAttempts);
    }

    [Theory]
    [InlineData("AsOf")]
    [InlineData("All")]
    [InlineData("Between")]
    [InlineData("FromTo")]
    [InlineData("ContainedIn")]
    public async Task ScalarTemporalProjection_RetainsHistoricalSql(string mode)
    {
        // Scenario: scalar historical projections need no independent roots and remain provider-translatable.
        using var db = new TemporalContext();
        var commands = new List<string>();
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        await transformer.RenderProjection(new ProjectionDirective(new[] { new ProjectionSelection("title") }), ProjectionFixture.Mapping())
            .ExecuteAsync(Temporal(db, mode).Where(p => p.Tenant == 1), new ProjectionSqlExecutor(q => commands.Add(q.ToQueryString())));
        Assert.Contains("FOR SYSTEM_TIME", Assert.Single(commands));
    }

    [Fact]
    public async Task CurrentQueryOverTemporalModel_RemainsSupported()
    {
        using var db = new TemporalContext();
        var commands = new List<string>();
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        await transformer.RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping())
            .ExecuteAsync(db.Set<ProjectionParent>(), new ProjectionSqlExecutor(q => commands.Add(q.ToQueryString())));
        Assert.Equal(3, commands.Count);
        Assert.All(commands, command => Assert.DoesNotContain("FOR SYSTEM_TIME", command));
    }

    public static IEnumerable<object[]> Scopes()
    {
        foreach (var mode in new[] { "AsOf", "All", "Between", "FromTo", "ContainedIn" })
        foreach (var explicitKeySource in new[] { false, true })
        foreach (var adapter in new[] { false, true })
            yield return new object[] { mode, explicitKeySource, adapter };
    }

    private static IQueryable<ProjectionParent> Temporal(TemporalContext db, string mode)
    {
        var from = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddDays(1);
        return mode switch
        {
            "AsOf" => db.Set<ProjectionParent>().TemporalAsOf(from),
            "All" => db.Set<ProjectionParent>().TemporalAll(),
            "Between" => db.Set<ProjectionParent>().TemporalBetween(from, to),
            "FromTo" => db.Set<ProjectionParent>().TemporalFromTo(from, to),
            _ => db.Set<ProjectionParent>().TemporalContainedIn(from, to)
        };
    }

    private sealed class TemporalContext : DbContext
    {
        internal TemporalContext(NoIo? noIo = null) : base(new DbContextOptionsBuilder<TemporalContext>().UseSqlServer("Server=unused;Database=unused")
            .AddInterceptors(noIo ?? new NoIo()).Options) { }
        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<ProjectionParent>().ToTable("Parents", t => t.IsTemporal()).HasKey(p => new { p.Tenant, p.Id });
            model.Entity<ProjectionChild>().ToTable("Children", t => t.IsTemporal()).HasKey(c => new { c.Tenant, c.Id });
            model.Entity<ProjectionLeaf>().ToTable("Leaves", t => t.IsTemporal()).HasKey(l => new { l.Tenant, l.Id });
            model.Entity<ProjectionParent>().HasMany(p => p.Children).WithOne().HasForeignKey(c => new { c.Tenant, c.ParentId });
            model.Entity<ProjectionChild>().HasMany(c => c.Leaves).WithOne().HasForeignKey(l => new { l.Tenant, l.ChildId });
        }
    }

    private sealed class NoIo : DbConnectionInterceptor
    {
        internal int OpenAttempts { get; private set; }
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
            OpenAttempts++;
            throw new Xunit.Sdk.XunitException("Unit tests must never open a database.");
        }
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            OpenAttempts++;
            throw new Xunit.Sdk.XunitException("Unit tests must never open a database.");
        }
    }
}
