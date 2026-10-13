using Expresso.Core.Projection;

namespace Expresso.Rendering.TestCases;

public abstract partial class ProjectionSqlTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnicodeNames_UseCatalogEqualityAtEveryDepth(bool uppercase)
    {
        // Scenario: Greek final sigma and sigma are ordinal-ignore-case equivalents, but not identical after lowercasing.
        const string name = "\u03ba\u03cc\u03c3\u03bc\u03bf\u03c2";
        var requested = uppercase ? name.ToUpperInvariant() : name.Substring(0, name.Length - 1) + "\u03c3";
        var equivalent = string.Equals(name, requested, StringComparison.OrdinalIgnoreCase);
        var child = new SqlProjectionMapping().Key("id", "c.id", typeof(int)).Field(name, "name", "c.name", typeof(string));
        var map = new SqlProjectionMapping().Key("id", "p.id", typeof(int)).Field(name, "title", "p.title", typeof(string))
            .Collection("children" + name, "children", new SqlCollectionProjectionMapping("child c", child,
                new[] { new SqlProjectionKey("id", "c.parent_id", typeof(int)) }));
        var directive = new ProjectionDirective(new[] { new ProjectionSelection(requested),
            new ProjectionSelection("children" + requested, new[] { new ProjectionSelection(requested) }) });
        if (!equivalent)
        {
            // .NET Framework's ordinal comparer has different Unicode folds; do not broaden its catalog semantics.
            Assert.Throws<ArgumentException>(() => _renderer.RenderProjection(directive, map));
            return;
        }
        var result = _renderer.RenderProjection(directive, map);
        Assert.Equal("title", Assert.Single(result.Fields).OutputName);
        Assert.Equal("name", Assert.Single(Assert.Single(result.Collections).Items.Fields).OutputName);
        var error = Assert.Throws<ArgumentException>(() => _renderer.RenderProjection(new ProjectionDirective(new[] {
            new ProjectionSelection(requested, new[] { new ProjectionSelection("nested") }) }), map));
        Assert.Contains("Cannot traverse a scalar", error.Message);
    }
}
