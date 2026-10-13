using Expresso.Core.Filtering;

namespace Expresso.Parsing.Test;

public sealed class ProjectionCatalogTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("children")]
    [InlineData("children,children.name")]
    [InlineData("children.name,children")]
    public void DefaultExpansion_IsBoundedByTheSuppliedCatalog(string? input)
    {
        // Scenario: the renderer has more fields than this endpoint; its default must not escape the response allow-list.
        var model = new QueryModel(new[] { ("title", typeof(string)) }, new[] {
            new CollectionModel("children", new QueryModel(new[] { ("name", typeof(string)) })) });
        var projection = new ProjectionDirectiveParser().Parse(input, model);
        Assert.False(projection.IsAll);
        Assert.Equal(input is null ? new[] { "children", "title" } : new[] { "children" }, projection.Selections.Select(s => s.Name));
        var children = projection.Find("children")!.ToDirective();
        Assert.False(children.IsAll);
        Assert.Equal("name", Assert.Single(children.Selections).Name);
        Assert.Throws<ArgumentException>(() => new ProjectionDirectiveParser().Parse("children.leaves", model));
    }

    [Fact]
    public void EmptyCatalogAndEmptyCollection_SelectNoUnlistedValues()
    {
        var parser = new ProjectionDirectiveParser();
        Assert.Same(Expresso.Core.Projection.ProjectionDirective.Empty, parser.Parse(null, QueryModel.Empty));
        Assert.Throws<ArgumentException>(() => parser.Parse("title", QueryModel.Empty));
        var model = new QueryModel(null, new[] { new CollectionModel("children", QueryModel.Empty) });
        foreach (var input in new string?[] { null, "children", "children,children" })
        {
            var selected = Assert.Single(parser.Parse(input, model).Selections);
            Assert.True(selected.IsCollectionSubtree);
            Assert.Same(Expresso.Core.Projection.ProjectionDirective.Empty, selected.ToDirective());
        }
    }
}
