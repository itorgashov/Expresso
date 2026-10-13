using System.Linq.Expressions;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.Linq.Test;

public sealed class ProjectionEntitySourceTests
{
    private static readonly (string, LambdaExpression)[] Keys = {
        ("tenant", (Expression<Func<BinaryProjectionParent, byte[]>>)(x => x.Tenant)),
        ("id", (Expression<Func<BinaryProjectionParent, int>>)(x => x.Id)) };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unique_PreservesScope_UsingEveryStructuralKey(bool independentSource)
    {
        // Scenario: authorization excludes another tenant and another id; cloned arrays must still match the allowed identity.
        var allowed = new BinaryProjectionParent { Tenant = new byte[] { 1 }, Id = 1 };
        var source = new[] { allowed, new BinaryProjectionParent { Tenant = new byte[] { 1 }, Id = 1 } }.AsQueryable();
        var entities = new[] { allowed, new BinaryProjectionParent { Tenant = new byte[] { 2 }, Id = 1 },
            new BinaryProjectionParent { Tenant = new byte[] { 1 }, Id = 2 },
            new BinaryProjectionParent { Tenant = null!, Id = 1 } }.AsQueryable();
        var result = ProjectionParentSource.Unique(source, Keys, new Sources(independentSource ? entities : null));
        Assert.Same(allowed, Assert.Single(result));
        Assert.DoesNotContain("Distinct", result.Expression.ToString());
        if (independentSource) Assert.Contains("Any", result.Expression.ToString());
    }

    [Fact]
    public void AlreadyUniqueScope_IsRetained_AndWrongEntitySourceIsRejected()
    {
        var source = BinaryProjectionFixture.Data();
        Assert.Same(source, ProjectionParentSource.Unique(source, Keys, new Sources(source)));
        Assert.Throws<ArgumentException>(() => ProjectionParentSource.Unique(source, Keys, new Sources(new[] { 1 }.AsQueryable())));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FirstLevelFallback_RetainsCallerSource_WhenNoResolverReturnsEntities(bool decliningResolver)
    {
        var source = BinaryProjectionFixture.Data();
        var provider = decliningResolver ? new Sources(null) : null;
        Assert.Same(source, ProjectionParentSource.Unique(source, Keys, provider, normalizeFallback: false));
        Assert.Single(ProjectionParentSource.Unique(source.Take(1), Keys, new Sources(source), normalizeFallback: false));
        Assert.Throws<ArgumentException>(() => ProjectionParentSource.Unique(source, Keys, new Sources(new[] { 1 }.AsQueryable()), normalizeFallback: false));
    }

    [Fact]
    public void SingleKey_AndNullBinaryKeys_AreComparedWithoutNonKeyColumns()
    {
        var keys = new[] { ("tenant", (LambdaExpression)(Expression<Func<BinaryProjectionParent, byte[]>>)(x => x.Tenant)) };
        var source = new[] { new BinaryProjectionParent { Tenant = null! } }.AsQueryable();
        Assert.Empty(ProjectionParentSource.Unique(source, keys, new Sources(BinaryProjectionFixture.Data())));
        var valid = BinaryProjectionFixture.Data().Take(1);
        Assert.Single(ProjectionParentSource.Unique(valid, keys, new Sources(BinaryProjectionFixture.Data())));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecutorCapability_TakesPrecedence_ButNullFallsBackToRenderer(bool resolves)
    {
        var data = ProjectionFixture.Data().AsQueryable();
        var renderer = new Sources(null, sameSource: true);
        var plan = new LinqProjectionPlan<ProjectionParent, ProjectionParentDto>(ProjectionFixture.Nested(), ProjectionFixture.Mapping(), renderer);
        var executor = new SourceExecutor(resolves);
        var result = await plan.ExecuteAsync(data, executor);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(2, executor.Resolutions);
        Assert.Equal(resolves ? 0 : 2, renderer.Resolutions);
    }

    private sealed class Sources : IProjectionEntitySourceProvider
    {
        private readonly IQueryable? _entities;
        private readonly bool _sameSource;
        internal int Resolutions { get; private set; }
        internal Sources(IQueryable? entities, bool sameSource = false) { _entities = entities; _sameSource = sameSource; }
        public IQueryable? GetEntitySource(IQueryable source, Type entityType)
        { Resolutions++; return _sameSource ? source : _entities; }
    }
    private sealed class SourceExecutor : IProjectionQueryExecutor, IProjectionEntitySourceProvider
    {
        private readonly bool _resolves;
        internal int Resolutions { get; private set; }
        internal SourceExecutor(bool resolves) => _resolves = resolves;
        public IQueryable? GetEntitySource(IQueryable source, Type entityType)
        { Resolutions++; return _resolves ? source : null; }
        public Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<T>>(query.ToList());
    }
}
