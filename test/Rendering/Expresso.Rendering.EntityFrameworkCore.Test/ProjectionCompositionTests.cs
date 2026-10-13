using Expresso.Core.Projection;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed partial class ProjectionTemporalTests
{
    [Theory]
    [MemberData(nameof(CompositionScopes))]
    public async Task CurrentComposition_RetainsSparseGraph(string shape, bool explicitKeys, bool adapter)
    {
        // Scenario: mapped EF properties and query-defined collections are safe; validate original SQL before mocked I/O.
        var noIo = new NoIo();
        using var db = new TemporalContext(noIo);
        var source = CompositionScope(db, shape);
        Assert.Contains(shape == "left" ? "LEFT JOIN" : shape == "join" ? "INNER JOIN" : "EXISTS", source.Select(p => p.Title).ToQueryString());
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var keySource = explicitKeys ? db.Set<ProjectionParent>() : null;
        if (adapter)
        {
            var error = await Assert.ThrowsAsync<Xunit.Sdk.XunitException>(() => source.ToProjectedListAsync(
                transformer, ProjectionFixture.Nested(), ProjectionFixture.Mapping(), keySource: keySource));
            Assert.Contains("must never open", error.Message);
            Assert.Equal(1, noIo.OpenAttempts); // The positive adapter reaches the mocked connection boundary, not a database.
            return;
        }
        var commands = new List<string>();
        var executor = new PropertyProjectionExecutor(commands);
        var result = await transformer.RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping())
            .ExecuteAsync(source, executor, keySource: keySource);
        Assert.Equal(shape == "left" ? 4 : shape == "join" ? 3 : 2, result.Items.Count);
        Assert.Equal(3, commands.Count);
        Assert.All(commands, sql => Assert.DoesNotContain("FOR SYSTEM_TIME", sql));
        Assert.Equal("first", result.Items[0].Children[0].Leaves[0].Value);
        var sparse = ProjectionShaper.ToSparseObjects(result);
        Assert.DoesNotContain("notes", sparse[0].Keys);
        Assert.Equal(0, noIo.OpenAttempts);
    }

    public static IEnumerable<object[]> CompositionScopes()
    {
        foreach (var shape in new[] { "property", "left", "join", "projected", "nested", "initialized" })
        foreach (var explicitKeys in new[] { false, true })
        foreach (var adapter in new[] { false, true })
            yield return new object[] { shape, explicitKeys, adapter };
    }

    private static IQueryable<ProjectionParent> CompositionScope(TemporalContext db, string shape)
    {
        var parents = db.Set<ProjectionParent>();
        return shape switch
        {
            "property" => parents.Where(p => EF.Property<List<ProjectionChild>>(p, "Children").Any(c => c.Id > 0)),
            "left" => from p in parents
                      join c in db.Set<ProjectionChild>() on new { p.Tenant, p.Id } equals new { c.Tenant, Id = c.ParentId } into groupItems
                      from c in groupItems.DefaultIfEmpty()
                      select p,
            "join" => parents.Join(db.Set<ProjectionChild>(), p => new { p.Tenant, p.Id }, c => new { c.Tenant, Id = c.ParentId }, (p, c) => p),
            "nested" => parents.Select(p => new { Wrapper = new { Parent = p, Kids = p.Children } })
                .Where(x => x.Wrapper.Kids.Any()).Select(x => x.Wrapper.Parent),
            "initialized" => parents.Select(p => new Intermediate { Parent = p, Kids = p.Children })
                .Where(x => x.Kids.Any()).Select(x => x.Parent),
            _ => parents.Select(p => new { Parent = p, Kids = p.Children })
                .Where(x => x.Kids.Any()).Select(x => x.Parent)
        };
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task EfPropertyMappings_MatchMemberNavigations(bool explicitKeys, bool adapter)
    {
        // Scenario: selected mappings access a provider intrinsic instead of a CLR member; preserve first-level SQL and output.
        var noIo = new NoIo();
        using var db = new TemporalContext(noIo);
        var child = new LinqProjectionMapping<ProjectionChild, ProjectionChildDto>().Key("tenant", c => c.Tenant).Key("id", c => c.Id)
            .Field("name", "name", c => c.Name, c => c.Name);
        var mapping = new LinqProjectionMapping<ProjectionParent, ProjectionParentDto>().Key("tenant", p => p.Tenant).Key("id", p => p.Id)
            .Field("title", "title", p => p.Title, p => p.Title)
            .Collection("children", "children", p => EF.Property<List<ProjectionChild>>(p, "Children"), p => p.Children,
                child, new LinqQueryMapping<ProjectionChild>());
        var source = db.Set<ProjectionParent>();
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        if (adapter)
        {
            await Assert.ThrowsAsync<Xunit.Sdk.XunitException>(() => source.ToProjectedListAsync(transformer,
                ProjectionDirective.All, mapping, keySource: explicitKeys ? source : null));
            Assert.Equal(1, noIo.OpenAttempts);
            return;
        }
        var commands = new List<string>();
        var result = await transformer.RenderProjection(ProjectionDirective.All, mapping)
            .ExecuteAsync(source, new PropertyProjectionExecutor(commands), keySource: explicitKeys ? source : null);
        Assert.Equal("beta", result.Items[0].Children[0].Name);
        Assert.Empty(result.Items[2].Children);
        Assert.Contains("JOIN [Children]", commands[1]);
        Assert.Equal(2, commands.Count);
        Assert.Equal(0, noIo.OpenAttempts);
    }

    private sealed class Intermediate
    {
        public ProjectionParent Parent { get; set; } = null!;
        public List<ProjectionChild> Kids { get; set; } = null!;
        public IEnumerable<ProjectionChild> Hidden => throw new Xunit.Sdk.XunitException("Do not invoke application getters.");
    }

    private sealed class PropertyProjectionExecutor : IProjectionQueryExecutor
    {
        private readonly List<string> _commands;
        private readonly ProjectionSqlExecutor _memory = new(_ => { });
        internal PropertyProjectionExecutor(List<string> commands) => _commands = commands;
        public Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            _commands.Add(query.ToQueryString()); // Translate the untouched production expression first.
            var expression = new PropertyAccess().Visit(query.Expression)!;
            return _memory.ToListAsync(query.Provider.CreateQuery<T>(expression), cancellationToken);
        }
    }

    private sealed class PropertyAccess : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node) =>
            node.Method.DeclaringType == typeof(EF) && node.Method.Name == nameof(EF.Property)
                ? Expression.Property(Visit(node.Arguments[0]), (string)((ConstantExpression)node.Arguments[1]).Value!)
                : base.VisitMethodCall(node);
    }

    [Fact]
    public void StructuralComposition_DoesNotTrustUnmappedOrUndefinedMembers()
    {
        // Scenario: projection provenance must not whitelist arbitrary getters or unmapped EF property names.
        using var db = new TemporalContext();
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(db.Set<ProjectionParent>()
            .Where(p => EF.Property<List<ProjectionChild>>(p, "Missing").Any())));
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(db.Set<ProjectionParent>()
            .Select(p => new Intermediate { Parent = p, Kids = p.Children }).Where(x => x.Hidden.Any()).Select(x => x.Parent)));
        var history = Temporal(db, "AsOf");
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(db.Set<ProjectionParent>()
            .Select(p => new { Parent = p, Kids = history }).SelectMany(x => x.Kids)));
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(db.Set<ProjectionParent>()
            .GroupJoin(history, p => p.Id, p => p.Id, (p, group) => new { Parent = p, Kids = group })
            .SelectMany(x => x.Kids.DefaultIfEmpty(), (x, _) => x.Parent)));
    }
}
