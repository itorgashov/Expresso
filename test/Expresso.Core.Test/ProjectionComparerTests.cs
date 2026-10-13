using System.Collections;
using Expresso.Core.Projection;

namespace Expresso.Core.Test;

public sealed class ProjectionComparerTests
{
    [Fact]
    public void EmptyAndBoundedSubtrees_AreDistinctFromMappingDefaults()
    {
        Assert.False(ProjectionDirective.Empty.IsAll);
        Assert.Empty(ProjectionDirective.Empty.Selections);
        Assert.Null(ProjectionDirective.Empty.Find("anything"));
        var empty = ProjectionSelection.Subtree("Children", ProjectionDirective.Empty);
        Assert.True(empty.IsCollectionSubtree);
        Assert.False(empty.SelectDefaultSubtree);
        Assert.Same(ProjectionDirective.Empty, empty.ToDirective());
        Assert.Same(ProjectionDirective.All, ProjectionSelection.Subtree("children", ProjectionDirective.All).ToDirective());
        var explicitTree = new ProjectionDirective(new[] { new ProjectionSelection("name") });
        Assert.Same(explicitTree, ProjectionSelection.Subtree("children", explicitTree).ToDirective());
        Assert.Throws<ArgumentNullException>(() => ProjectionSelection.Subtree("children", null!));
        Assert.Throws<ArgumentException>(() => ProjectionSelection.Subtree("bad.name", ProjectionDirective.Empty));
    }

    [Fact]
    public void ComponentEquality_IsImmutableStructuralAndHashConsistent()
    {
        // Scenario: one case-insensitive text component, binary and nullable structural components; preserve source values.
        var rules = new IEqualityComparer?[] { StringComparer.OrdinalIgnoreCase, null, null };
        var comparer = new CompositeProjectionKeyComparer(rules);
        rules[0] = StringComparer.Ordinal;
        var upper = new CompositeProjectionKey(new object?[] { "PARENT-A", new byte[] { 1, 2 }, null });
        var lower = new CompositeProjectionKey(new object?[] { "parent-a", new byte[] { 1, 2 }, null });
        Assert.Equal(3, comparer.ComponentCount);
        Assert.True(comparer.Equals(upper, lower));
        Assert.Equal(comparer.GetHashCode(upper), comparer.GetHashCode(lower));
        Assert.Equal("PARENT-A", upper[0]);
        Assert.False(upper.Equals(lower));
        Assert.False(comparer.Equals(upper, new CompositeProjectionKey(new object?[] { "parent-a", new byte[] { 1, 3 }, null })));
        Assert.False(comparer.Equals(upper, new CompositeProjectionKey(new object?[] { "other", new byte[] { 1, 2 }, null })));
        Assert.True(comparer.Equals(null, null));
        Assert.False(comparer.Equals(upper, null));
        Assert.False(comparer.Equals(null, upper));
        Assert.Throws<ArgumentNullException>(() => new CompositeProjectionKeyComparer(null!));
        Assert.Throws<ArgumentException>(() => new CompositeProjectionKeyComparer());
        Assert.Throws<ArgumentNullException>(() => comparer.GetHashCode(null!));
        var wrongShape = new CompositeProjectionKey(new object?[] { "parent-a" });
        Assert.Throws<ArgumentException>(() => comparer.Equals(upper, wrongShape));
        Assert.Throws<ArgumentException>(() => comparer.Equals(wrongShape, upper));
        Assert.Throws<ArgumentException>(() => comparer.GetHashCode(wrongShape));
    }
}
