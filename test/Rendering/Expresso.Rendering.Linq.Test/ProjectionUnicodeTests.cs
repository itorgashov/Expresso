using Expresso.Core.Projection;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.Linq.Test;

public sealed class ProjectionUnicodeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnicodeNames_ValidateAndShapeUsingCatalogEquality(bool uppercase)
    {
        // Scenario: equivalent non-ASCII names must validate, select and shape identically at root and collection levels.
        const string name = "\u03ba\u03cc\u03c3\u03bc\u03bf\u03c2";
        var requested = uppercase ? name.ToUpperInvariant() : name.Substring(0, name.Length - 1) + "\u03c3";
        var child = new LinqProjectionMapping<ProjectionChild, ProjectionChildDto>().Key("tenant", c => c.Tenant).Key("id", c => c.Id)
            .Field(name, "name", c => c.Name, c => c.Name);
        var map = new LinqProjectionMapping<ProjectionParent, ProjectionParentDto>().Key("tenant", p => p.Tenant).Key("id", p => p.Id)
            .Field(name, "title", p => p.Title, p => p.Title)
            .Collection("children" + name, "children", p => p.Children, p => p.Children, child, new LinqQueryMapping<ProjectionChild>());
        var equivalent = string.Equals(name, requested, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(equivalent, map.GetProjectionModel().TryGetField(requested, out _));
        Assert.Equal(equivalent, map.GetProjectionModel().TryGetCollection("children" + requested, out _));
        var directive = new ProjectionDirective(new[] { new ProjectionSelection(requested),
            new ProjectionSelection("children" + requested, new[] { new ProjectionSelection(requested) }) });
        var transformer = new QueryableExpressionToLinqTransformer();
        if (!equivalent)
        {
            Assert.Throws<ArgumentException>(() => transformer.RenderProjection(directive, map));
            return;
        }
        var result = await transformer.RenderProjection(directive, map).ExecuteAsync(ProjectionFixture.Data().AsQueryable(), new MemoryProjectionExecutor());
        Assert.Equal("one", result.Items[0].Title);
        Assert.Equal("beta", result.Items[0].Children[0].Name);
        Assert.Equal(new[] { "children", "title" }, ProjectionShaper.ToSparseObjects(result)[0].Keys.OrderBy(k => k));
        var error = Assert.Throws<ArgumentException>(() => transformer.RenderProjection(new ProjectionDirective(new[] {
            new ProjectionSelection(requested, new[] { new ProjectionSelection("nested") }) }), map));
        Assert.Contains("Cannot traverse a scalar", error.Message);
    }
}
