using Expresso.Core.Projection;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed partial class ProjectionTemporalTests
{
    [Theory]
    [MemberData(nameof(SavedMappingScopes))]
    public async Task SavedMappedExpressions_RejectAtEveryDepth(bool deep, bool empty, bool adapter, bool explicitKeys)
    {
        // Scenario: saved predicates hide history only inside selected mappings; reject before reading even an empty current root.
        var noIo = new NoIo();
        using var db = new TemporalContext(noIo);
        var historicalChildren = db.Set<ProjectionChild>().TemporalAsOf(new DateTime(2020, 1, 1));
        var historicalLeaves = db.Set<ProjectionLeaf>().TemporalAsOf(new DateTime(2020, 1, 1));
        var mapping = SavedMapping(historicalChildren, historicalLeaves, deep);
        var parents = db.Set<ProjectionParent>();
        var source = empty ? parents.Where(p => p.Id < 0) : parents;
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var executor = new ProjectionSqlExecutor(_ => throw new Xunit.Sdk.XunitException("Unexpected root read."));
        await Assert.ThrowsAsync<NotSupportedException>(() => adapter
            ? source.ToProjectedListAsync(transformer, ProjectionFixture.Nested(), mapping, keySource: explicitKeys ? parents : null)
            : transformer.RenderProjection(ProjectionFixture.Nested(), mapping).ExecuteAsync(source, executor, keySource: explicitKeys ? parents : null));
        Assert.Empty(executor.RowCounts);
        Assert.Equal(0, noIo.OpenAttempts);
    }

    public static IEnumerable<object[]> SavedMappingScopes()
    {
        foreach (var deep in new[] { false, true })
        foreach (var empty in new[] { false, true })
        foreach (var adapter in new[] { false, true })
        foreach (var explicitKeys in new[] { false, true })
            yield return new object[] { deep, empty, adapter, explicitKeys };
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnselectedSavedMappingHistory_RemainsUnused(bool deep)
    {
        using var db = new TemporalContext();
        var mapping = SavedMapping(db.Set<ProjectionChild>().TemporalAsOf(new DateTime(2020, 1, 1)),
            db.Set<ProjectionLeaf>().TemporalAsOf(new DateTime(2020, 1, 1)), deep);
        var commands = new List<string>();
        var result = await new EfCoreExpressionToLinqTransformer(db.Database.ProviderName)
            .RenderProjection(new ProjectionDirective(new[] { new ProjectionSelection("title") }), mapping)
            .ExecuteAsync(db.Set<ProjectionParent>(), new StageExecutor(commands));
        Assert.Equal(3, result.Items.Count);
        Assert.DoesNotContain("FOR SYSTEM_TIME", Assert.Single(commands));
    }

    private static LinqProjectionMapping<ProjectionParent, ProjectionParentDto> SavedMapping(
        IQueryable<ProjectionChild> historicalChildren, IQueryable<ProjectionLeaf> historicalLeaves, bool deep)
    {
        Expression<Func<ProjectionChild, bool>> childPredicate = c => historicalChildren.Any(h => h.Id == c.Id);
        Expression<Func<ProjectionLeaf, bool>> leafPredicate = l => historicalLeaves.Any(h => h.Id == l.Id);
        var leaves = new LinqProjectionMapping<ProjectionLeaf, ProjectionLeafDto>().Key("tenant", l => l.Tenant).Key("id", l => l.Id)
            .Field("value", "value", l => l.Value, l => l.Value);
        var children = new LinqProjectionMapping<ProjectionChild, ProjectionChildDto>().Key("tenant", c => c.Tenant).Key("id", c => c.Id)
            .Field("name", "name", c => c.Name, c => c.Name);
        Expression<Func<ProjectionChild, IEnumerable<ProjectionLeaf>>> leafScope = c => c.Leaves;
        if (deep) leafScope = c => c.Leaves.AsQueryable().Where(leafPredicate);
        children.Collection("leaves", "leaves", leafScope, c => c.Leaves, leaves, new LinqQueryMapping<ProjectionLeaf>());
        Expression<Func<ProjectionParent, IEnumerable<ProjectionChild>>> childScope = p => p.Children;
        if (!deep) childScope = p => p.Children.AsQueryable().Where(childPredicate);
        return new LinqProjectionMapping<ProjectionParent, ProjectionParentDto>().Key("tenant", p => p.Tenant).Key("id", p => p.Id)
            .Field("title", "title", p => p.Title, p => p.Title)
            .Collection("children", "children", childScope, p => p.Children, children, new LinqQueryMapping<ProjectionChild>());
    }

    [Fact]
    public void StoredExpressionValues_RejectNullCyclesAndOpaqueConversionsWithoutEvaluation()
    {
        // Scenario: safe storage does not imply a valid finite lambda; recursion/null/unseen casts fail, not execute or loop.
        using var db = new TemporalContext();
        var parents = db.Set<ProjectionParent>();
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        Expression<Func<ProjectionParent, bool>>? missing = null;
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(parents.SelectMany(_ => parents.Where(missing!))));
        Expression<Func<ProjectionParent, bool>> recursive = null!;
        recursive = p => parents.Where(recursive).Any();
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(parents.SelectMany(_ => parents.Where(recursive))));
        object boxed = (Expression<Func<ProjectionParent, bool>>)(p => p.Children.Any());
        transformer.ValidateSource(parents.SelectMany(_ => parents.Where((Expression<Func<ProjectionParent, bool>>)boxed)));
        var lambda = (Expression<Func<ProjectionParent, bool>>)(p => Temporal(db, "AsOf").Any());
        var holder = new QueryValue(parents) { Expression = Expression.Constant(lambda, typeof(Expression<Func<ProjectionParent, bool>>)) };
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(holder));
    }
}
