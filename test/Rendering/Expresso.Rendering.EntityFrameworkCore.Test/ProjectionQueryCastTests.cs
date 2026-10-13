using Expresso.Core.Projection;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed partial class ProjectionTemporalTests
{
    [Theory]
    [MemberData(nameof(QueryCastScopes))]
    public async Task QueryCasts_RetainKnownMappedRows(string shape, bool tpt, bool adapter, bool explicitKeys)
    {
        // Scenario: widened interface/object query rows keep concrete metadata across filters/wrappers; SQL and sparse graphs remain valid.
        var noIo = new NoIo();
        using var db = tpt ? new CastTptContext(noIo) : new CastContext(noIo);
        var nodes = db.Set<CastNode>();
        var source = QueryCastSource(nodes, shape);
        Assert.Contains("EXISTS", source.ToQueryString());
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        if (adapter)
        {
            await Assert.ThrowsAsync<Xunit.Sdk.XunitException>(() => source.ToProjectedListAsync(transformer,
                ProjectionDirective.All, DerivedCastProjectionFixture.Mapping(), keySource: explicitKeys ? nodes : null));
            Assert.Equal(1, noIo.OpenAttempts);
            return;
        }
        var commands = new List<string>();
        var memory = new ProjectionSqlExecutor<CastNode>(DerivedCastProjectionFixture.Data().AsQueryable(), _ => { });
        var result = await transformer.RenderProjection(ProjectionDirective.All, DerivedCastProjectionFixture.Mapping())
            .ExecuteAsync(source, new CastExecutor(memory, commands), keySource: explicitKeys ? nodes : null);
        Assert.Equal("special", Assert.Single(result.Items).Name);
        Assert.Equal("item", Assert.Single(result.Items[0].Items).Name);
        Assert.Equal(2, commands.Count);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Equal(0, noIo.OpenAttempts);
    }

    public static IEnumerable<object[]> QueryCastScopes()
    {
        foreach (var shape in new[] { "interface", "property", "OfType", "object", "wrapper", "filtered", "repeated", "expression", "cast-back" })
        foreach (var tpt in new[] { false, true })
        foreach (var adapter in new[] { false, true })
        foreach (var keys in new[] { false, true })
            yield return new object[] { shape, tpt, adapter, keys };
    }

    private static IQueryable<CastNode> QueryCastSource(IQueryable<CastNode> nodes, string shape) => shape switch
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void QueryCasts_DoNotTrustUnmappedInterfaceMembers(bool property)
    {
        // Scenario: another entity maps Common, but this interface implementation does not; never infer metadata by member name.
        var noIo = new NoIo();
        using var db = new UnmappedCastContext(noIo);
        var rows = db.Set<UnmappedCastNode>().Cast<ICastNode>();
        var source = property ? rows.Where(n => EF.Property<List<CastItem>>(n, "Common").Any()) : rows.Where(n => n.Common.Any());
        Assert.Throws<NotSupportedException>(() => new EfCoreExpressionToLinqTransformer(db.Database.ProviderName).ValidateSource(source));
        Assert.Equal(0, noIo.OpenAttempts);
    }

    [Theory]
    [MemberData(nameof(QueryCastUnsafeScopes))]
    public async Task QueryCasts_RejectUnsafeScopesBeforeIo(string shape, bool adapter, bool explicitKeys)
    {
        // Scenario: widening casts cannot hide historical roots, unknown navigation names or opaque factories in root/key scopes.
        var noIo = new NoIo();
        using var db = new TemporalContext(noIo);
        var parents = db.Set<ProjectionParent>();
        var unsafeSource = shape == "temporal"
            ? Temporal(db, "AsOf").Cast<object>().Where(p => EF.Property<List<ProjectionChild>>(p, "Children").Any()).Cast<ProjectionParent>()
            : shape == "missing" ? parents.Cast<object>().Where(p => EF.Property<List<ProjectionChild>>(p, "Missing").Any()).Cast<ProjectionParent>()
            : parents.Cast<object>().Where(p => QueryCastFactory().Any()).Cast<ProjectionParent>();
        var source = explicitKeys ? parents : unsafeSource;
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var commands = new List<string>();
        if (adapter)
            await Assert.ThrowsAsync<NotSupportedException>(() => source.ToProjectedListAsync(transformer, ProjectionFixture.Nested(),
                ProjectionFixture.Mapping(), keySource: explicitKeys ? unsafeSource : null));
        else
            await Assert.ThrowsAsync<NotSupportedException>(() => transformer.RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping())
                .ExecuteAsync(source, new ProjectionSqlExecutor<ProjectionParent>(ProjectionFixture.Data().AsQueryable(), q => commands.Add(q.ToQueryString())),
                    keySource: explicitKeys ? unsafeSource : null));
        Assert.Empty(commands);
        Assert.Equal(0, noIo.OpenAttempts);
    }

    public static IEnumerable<object[]> QueryCastUnsafeScopes()
    {
        foreach (var shape in new[] { "temporal", "missing", "factory" })
        foreach (var adapter in new[] { false, true })
        foreach (var keys in new[] { false, true })
            yield return new object[] { shape, adapter, keys };
    }
    private static IEnumerable<ProjectionChild> QueryCastFactory() => throw new Xunit.Sdk.XunitException("Do not invoke query factories.");
    private sealed class UnmappedCastNode : ICastNode
    {
        public int Id { get; set; }
        public List<CastItem> Common { get => throw new Xunit.Sdk.XunitException("Do not invoke getters."); set { } }
    }
    private sealed class UnmappedCastContext : CastContext
    {
        internal UnmappedCastContext(NoIo noIo) : base(noIo) { }
        protected override void OnModelCreating(ModelBuilder model)
        {
            base.OnModelCreating(model);
            model.Entity<UnmappedCastNode>().Ignore(n => n.Common);
        }
    }
}
