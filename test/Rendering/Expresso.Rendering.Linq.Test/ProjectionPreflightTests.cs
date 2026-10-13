using Expresso.Core.Projection;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.Linq.Test;

public sealed class ProjectionPreflightTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SelectedScopes_AreValidatedRecursivelyBeforeRootRead(bool all, bool explicitKeys)
    {
        // Scenario: the executor refuses the deepest selected scope; every scope must be constructed/checked before any row is read.
        var source = ProjectionFixture.Data().AsQueryable();
        var executor = new ValidatingExecutor(typeof(ProjectionLeaf));
        var plan = new QueryableExpressionToLinqTransformer().RenderProjection(all ? ProjectionDirective.All : ProjectionFixture.Nested(), ProjectionFixture.Mapping());
        await Assert.ThrowsAsync<NotSupportedException>(() => plan.ExecuteAsync(source, executor, keySource: explicitKeys ? source : null));
        Assert.Equal(new[] { typeof(ProjectionParent), typeof(ProjectionChild), typeof(ProjectionLeaf) }, executor.Scopes.Distinct());
        Assert.Empty(executor.Memory.Queries);
    }

    [Fact]
    public async Task UnselectedDescendants_AreNotValidated_AndCurrentGraphStillLoads()
    {
        // Scenario: a selected intermediate collection omits the refused leaf; normal projection executes with unchanged sparse results.
        var executor = new ValidatingExecutor(typeof(ProjectionLeaf));
        var selection = new ProjectionDirective(new[] { ProjectionSelection.Subtree("children",
            new ProjectionDirective(new[] { new ProjectionSelection("name") })) });
        var result = await new QueryableExpressionToLinqTransformer().RenderProjection(selection, ProjectionFixture.Mapping())
            .ExecuteAsync(ProjectionFixture.Data().AsQueryable(), executor);
        Assert.DoesNotContain(typeof(ProjectionLeaf), executor.Scopes);
        Assert.Equal(2, executor.Memory.Queries.Count);
        var roots = ProjectionShaper.ToSparseObjects(result);
        Assert.Equal(new[] { "children" }, roots[0].Keys);
        Assert.Equal("beta", result.Items[0].Children[0].Name);
        Assert.Empty(result.Items[0].Children[0].Leaves);
    }

    [Fact]
    public async Task ScalarOnlyPlan_DoesNotRunCollectionCapabilityValidation()
    {
        var executor = new ValidatingExecutor(typeof(ProjectionParent));
        await new QueryableExpressionToLinqTransformer().RenderProjection(
            new ProjectionDirective(new[] { new ProjectionSelection("title") }), ProjectionFixture.Mapping())
            .ExecuteAsync(ProjectionFixture.Data().AsQueryable(), executor);
        Assert.Empty(executor.Scopes);
        Assert.Single(executor.Memory.Queries);
    }

    [Fact]
    public async Task EmptyRoots_DoNotDeferSelectedScopeValidation()
    {
        var executor = new ValidatingExecutor(typeof(ProjectionLeaf));
        await Assert.ThrowsAsync<NotSupportedException>(() => new QueryableExpressionToLinqTransformer()
            .RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping())
            .ExecuteAsync(Array.Empty<ProjectionParent>().AsQueryable(), executor));
        Assert.Empty(executor.Memory.Queries);
    }

    private sealed class ValidatingExecutor : IProjectionQueryExecutor, IProjectionSourceValidator
    {
        private readonly Type _reject;
        internal ValidatingExecutor(Type reject) { _reject = reject; }
        internal MemoryProjectionExecutor Memory { get; } = new();
        internal List<Type> Scopes { get; } = new();
        public void ValidateSource(IQueryable source)
        {
            Scopes.Add(source.ElementType);
            if (source.ElementType == _reject) throw new NotSupportedException("Unsupported test scope.");
        }
        public Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) => Memory.ToListAsync(query, cancellationToken);
    }
}
