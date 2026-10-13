using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Sorting;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.TestCases;

/// <summary>Three-level graph with stable defaults at both collection levels.</summary>
public static class OrderedProjectionFixture
{
    public static IQueryable<ProjectionParent> Data()
    {
        var data = ProjectionFixture.Data();
        data[0].Children[0].Leaves.Insert(0, new ProjectionLeaf { Tenant = 1, Id = 2, ChildId = 1, Value = "zulu" });
        return data.AsQueryable();
    }

    public static SortDirective Sort(bool explicitParent) => new SortDirective(Array.Empty<SortDirectiveItem>(), new[] {
        new CollectionSort("children", new SortDirective(explicitParent ? new[] { Item("name", SortDirection.Descending) } : Array.Empty<SortDirectiveItem>(),
            new[] { new CollectionSort("leaves", new SortDirective(new[] { Item("value", SortDirection.Descending) })) })) });

    public static LinqProjectionMapping<ProjectionParent, ProjectionParentDto> Mapping()
    {
        var leaves = new LinqProjectionMapping<ProjectionLeaf, ProjectionLeafDto>()
            .Key("tenant", x => x.Tenant).Key("id", x => x.Id).Field("value", "value", x => x.Value, x => x.Value);
        var children = new LinqProjectionMapping<ProjectionChild, ProjectionChildDto>()
            .Key("tenant", x => x.Tenant).Key("id", x => x.Id).Field("name", "name", x => x.Name, x => x.Name)
            .Collection("leaves", "leaves", x => x.Leaves, x => x.Leaves, leaves,
                new LinqQueryMapping<ProjectionLeaf>().Field("value", x => x.Value), new SortDirective(new[] { Item("value", SortDirection.Ascending) }));
        return new LinqProjectionMapping<ProjectionParent, ProjectionParentDto>()
            .Key("tenant", x => x.Tenant).Key("id", x => x.Id).Field("title", "title", x => x.Title, x => x.Title)
            .Collection("children", "children", x => x.Children, x => x.Children, children,
                new LinqQueryMapping<ProjectionChild>().Field("name", x => x.Name), new SortDirective(new[] { Item("name", SortDirection.Ascending) }));
    }

    private static SortDirectiveItem Item(string name, SortDirection direction) =>
        new SortDirectiveItem { Expression = new Field(name, typeof(string)), Direction = direction };
}
