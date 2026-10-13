using System.Linq.Expressions;
using Expresso.Core.Filtering;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed class ProjectionAdapterTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Adapter_UsesSuppliedProjectionTransformer_AndRetainsClauseOnlyFallback(bool clauseOnly)
    {
        // Scenario: custom DI transformer must handle projection; older clause-only implementations retain the standard fallback.
        var source = new AsyncProjectionQuery<ProjectionParent>(ProjectionFixture.Data());
        var recording = new RecordingTransformer();
        IExpressionToLinqTransformer transformer = clauseOnly ? new ClauseTransformer() : recording;
        var result = await source.ToProjectedListAsync(transformer, ProjectionFixture.Nested(), ProjectionFixture.Mapping(), keySource: source);
        Assert.Equal(clauseOnly ? 0 : 1, recording.Projections);
        Assert.Equal("first", result.Items[0].Children[0].Leaves[0].Value);
    }

    private class ClauseTransformer : IExpressionToLinqTransformer
    {
        private readonly QueryableExpressionToLinqTransformer _standard = new();
        public Expression<Func<T, bool>> BuildPredicate<T>(FilterCriteria filter, LinqQueryMapping<T> mapping) =>
            _standard.BuildPredicate(filter, mapping);
        public IReadOnlyList<LinqSortKey> BuildSortKeys<T>(SortDirective sort, LinqQueryMapping<T> mapping) =>
            _standard.BuildSortKeys(sort, mapping);
    }
    private sealed class RecordingTransformer : ClauseTransformer, IProjectionToLinqTransformer
    {
        internal int Projections { get; private set; }
        public LinqProjectionPlan<TEntity, TResult> RenderProjection<TEntity, TResult>(ProjectionDirective projection,
            LinqProjectionMapping<TEntity, TResult> mapping) where TResult : new()
        {
            Projections++;
            return new QueryableExpressionToLinqTransformer().RenderProjection(projection, mapping);
        }
    }
}
