#if NET8_0_OR_GREATER
using Expresso.Core.Projection;
using Expresso.Rendering.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.Integration.Test.Ef;

public sealed partial class ProjectionDerivedCastItTests
{
    [SkippableTheory]
    [MemberData(nameof(QueryCastScopes))]
    public async Task Projection_QueryCasts_KeepMappedGraph(string shape, bool tpt, bool explicitKeys, bool adapter)
    {
        // Scenario: widened query rows retain mapped navigations after filtering/wrappers; execute genuine TPH/TPT graphs with both key modes.
        Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var db = tpt ? new TptContext(connection) : new Context(connection);
        await db.Database.EnsureCreatedAsync(); // Only private in-memory widget_cast_* tables.
        var data = DerivedCastProjectionFixture.Data();
        var special = (CastSpecialNode)data[0];
        special.Target = null;
        special.TargetId = null;
        db.AddRange(data);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var nodes = db.Set<CastNode>();
        var source = shape switch
        {
            "property" => nodes.Cast<ICastNode>().Where(n => EF.Property<List<CastItem>>(n, "Common").Any()).Cast<CastNode>(),
            "OfType" => nodes.OfType<ICastNode>().Where(n => n.Common.Any()).Cast<CastNode>(),
            "object" => nodes.Cast<object>().Where(n => EF.Property<List<CastItem>>(n, "Common").Any()).Cast<CastNode>(),
            "wrapper" => nodes.Cast<ICastNode>().AsNoTracking().TagWith("query cast").Where(n => n.Common.Any()).Cast<CastNode>(),
            "filtered" => nodes.Where(n => n.Name == "special").Cast<ICastNode>().Where(n => n.Common.Any()).Cast<CastNode>(),
            "repeated" => nodes.Cast<ICastNode>().Where(n => n.Common.Any()).Where(n => n.Common.Count > 0).Cast<object>()
                .Where(n => EF.Property<List<CastItem>>(n, "Common").Any()).Cast<CastNode>(),
            "expression" => nodes.Where(n => ((ICastNode)n).Common.Any()),
            "cast-back" => nodes.Cast<object>().Where(n => ((CastNode)n).Common.Any()).Cast<CastNode>(),
            _ => nodes.Cast<ICastNode>().Where(n => n.Common.Any()).Cast<CastNode>()
        };
        Assert.Equal("special", Assert.Single(await source.Select(n => n.Name).ToListAsync()));
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var map = DerivedCastProjectionFixture.Mapping();
        var result = adapter ? await source.ToProjectedListAsync(transformer, ProjectionDirective.All, map, keySource: explicitKeys ? nodes : null)
            : await transformer.RenderProjection(ProjectionDirective.All, map).ExecuteAsync(source, new Executor(), keySource: explicitKeys ? nodes : null);
        Assert.Equal("special", Assert.Single(result.Items).Name);
        Assert.Equal("item", Assert.Single(result.Items[0].Items).Name);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    public static IEnumerable<object[]> QueryCastScopes()
    {
        foreach (var shape in new[] { "interface", "property", "OfType", "object", "wrapper", "filtered", "repeated", "expression", "cast-back" })
        foreach (var tpt in new[] { false, true })
        foreach (var keys in new[] { false, true })
        foreach (var adapter in new[] { false, true })
            yield return new object[] { shape, tpt, keys, adapter };
    }
}
#endif
