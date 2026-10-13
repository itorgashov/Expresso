using Expresso.Core.Projection;

namespace Expresso.Core.Test;

public sealed class ProjectionTests
{
    [Fact]
    public void Directive_IsImmutableNormalizedAndSorted()
    {
        var values = new[] { new ProjectionSelection("Year"), new ProjectionSelection("Title") };
        var directive = new ProjectionDirective(values);
        values[0] = new ProjectionSelection("changed");
        Assert.Equal(new[] { "title", "year" }, directive.Selections.Select(s => s.Name));
        Assert.Equal("title", directive.Find("TITLE")!.Name);
        Assert.Null(directive.Find("missing"));
        Assert.True(ProjectionDirective.All.IsAll);
        Assert.Empty(ProjectionDirective.All.Selections);
        Assert.Throws<NotSupportedException>(() => ((IList<ProjectionSelection>)directive.Selections).Add(new ProjectionSelection("extra")));
    }

    [Fact]
    public void InvalidModelsFailEarly()
    {
        Assert.Throws<ArgumentNullException>(() => new ProjectionDirective(null!));
        Assert.Throws<ArgumentException>(() => new ProjectionDirective(Array.Empty<ProjectionSelection>()));
        Assert.Throws<ArgumentException>(() => new ProjectionDirective(new[] { new ProjectionSelection("x"), new ProjectionSelection("X") }));
        Assert.Throws<ArgumentException>(() => new ProjectionDirective(new ProjectionSelection[] { null! }));
        foreach (var name in new[] { "", " ", "a.b", "a,b", "a b", "a\tb", "a\rb", "a\nb" })
            Assert.Throws<ArgumentException>(() => new ProjectionSelection(name));
        Assert.Throws<ArgumentException>(() => new ProjectionSelection("a", new[] { new ProjectionSelection("x") }, true));
        Assert.Same(ProjectionDirective.All, new ProjectionSelection("a").ToDirective());
        Assert.Same(ProjectionDirective.All, new ProjectionSelection("a", selectDefaultSubtree: true).ToDirective());
        Assert.Single(new ProjectionSelection("a", new[] { new ProjectionSelection("x") }).ToDirective().Selections);
    }

    [Fact]
    public void CompositeKey_UsesAllComponentsAndCopiesArrays()
    {
        var bytes = new byte[] { 1, 2 };
        var key = new CompositeProjectionKey(new object?[] { 7, bytes });
        bytes[0] = 8;
        Assert.Equal(new CompositeProjectionKey(new object?[] { 7, new byte[] { 1, 2 } }), key);
        Assert.Equal(key.GetHashCode(), new CompositeProjectionKey(new object?[] { 7, new byte[] { 1, 2 } }).GetHashCode());
        Assert.NotEqual(new CompositeProjectionKey(new object?[] { 8, new byte[] { 1, 2 } }), key);
        Assert.False(key.Equals((object)"other"));
        Assert.True(key.Equals((object)new CompositeProjectionKey(new object?[] { 7, new byte[] { 1, 2 } })));
        Assert.False(key.Equals((CompositeProjectionKey?)null));
        ((byte[])key[1]!)[0] = 9;
        Assert.Equal(1, ((byte[])key[1]!)[0]);
        Assert.Equal(2, key.Count);
        Assert.True(key.IsComplete);
        Assert.False(new CompositeProjectionKey(new object?[] { null }).IsComplete);
        Assert.Throws<ArgumentNullException>(() => new CompositeProjectionKey(null!));
        Assert.Throws<ArgumentException>(() => new CompositeProjectionKey(Array.Empty<object>()));
    }
}
