using System.Linq.Expressions;
using Expresso.Core.Projection;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.TestCases;

/// <summary>A three-level composite-key graph with repeated individual key values.</summary>
public static class ProjectionFixture
{
    public static List<ProjectionParent> Data() => new List<ProjectionParent>
    {
        new ProjectionParent { Tenant = 1, Id = 1, Title = "one", Children = new List<ProjectionChild> {
            new ProjectionChild { Tenant = 1, Id = 1, ParentId = 1, Name = "beta", Leaves = new List<ProjectionLeaf> {
                new ProjectionLeaf { Tenant = 1, Id = 1, ChildId = 1, Value = "first" } } },
            new ProjectionChild { Tenant = 1, Id = 2, ParentId = 1, Name = "alpha" } } },
        new ProjectionParent { Tenant = 2, Id = 1, Title = "two", Notes = "note", Children = new List<ProjectionChild> {
            new ProjectionChild { Tenant = 2, Id = 1, ParentId = 1, Name = "gamma", Leaves = new List<ProjectionLeaf> {
                new ProjectionLeaf { Tenant = 2, Id = 1, ChildId = 1, Value = "second" } } } } },
        new ProjectionParent { Tenant = 1, Id = 2, Title = "empty" }
    };

    public static LinqProjectionMapping<ProjectionParent, ProjectionParentDto> Mapping()
    {
        var leaves = new LinqProjectionMapping<ProjectionLeaf, ProjectionLeafDto>()
            .Key("tenant", x => x.Tenant).Key("id", x => x.Id)
            .Field("value", "value", x => x.Value, x => x.Value);
        var children = new LinqProjectionMapping<ProjectionChild, ProjectionChildDto>()
            .Key("tenant", x => x.Tenant).Key("id", x => x.Id)
            .Field("name", "name", x => x.Name, x => x.Name)
            .Collection("leaves", "leaves", x => x.Leaves, x => x.Leaves, leaves, new LinqQueryMapping<ProjectionLeaf>().Field("value", x => x.Value));
        return new LinqProjectionMapping<ProjectionParent, ProjectionParentDto>()
            .Key("tenant", x => x.Tenant).Key("id", x => x.Id)
            .Field("id", "id", x => x.Id, x => x.Id)
            .Field("title", "title", x => x.Title, x => x.Title)
            .Field("notes", "notes", x => x.Notes, x => x.Notes)
            .Collection("children", "children", x => x.Children, x => x.Children, children,
                new LinqQueryMapping<ProjectionChild>().Field("name", x => x.Name));
    }

    public static LinqProjectionMapping<ProjectionParent, ProjectionParentDto> FirstLevelMapping() =>
        new LinqProjectionMapping<ProjectionParent, ProjectionParentDto>()
            .Key("tenant", x => x.Tenant).Key("id", x => x.Id)
            .Field("title", "title", x => x.Title, x => x.Title)
            .Collection("children", "children", x => x.Children, x => x.Children,
                new LinqProjectionMapping<ProjectionChild, ProjectionChildDto>()
                    .Key("tenant", x => x.Tenant).Key("id", x => x.Id)
                    .Field("name", "name", x => x.Name, x => x.Name), new LinqQueryMapping<ProjectionChild>());

    public static ProjectionDirective Nested() => new ProjectionDirective(new[] {
        new ProjectionSelection("title"), new ProjectionSelection("children", new[] {
            new ProjectionSelection("name"), new ProjectionSelection("leaves", new[] { new ProjectionSelection("value") }) }) });
}
public sealed class ProjectionParent
{
    public int Tenant { get; set; }
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
    public List<ProjectionChild> Children { get; set; } = new List<ProjectionChild>();
}
public sealed class ProjectionChild
{
    public int Tenant { get; set; }
    public int Id { get; set; }
    public int ParentId { get; set; }
    public string Name { get; set; } = "";
    public string? Payload { get; set; }
    public List<ProjectionLeaf> Leaves { get; set; } = new List<ProjectionLeaf>();
}
public sealed class ProjectionLeaf
{
    public int Tenant { get; set; }
    public int Id { get; set; }
    public int ChildId { get; set; }
    public string Value { get; set; } = "";
}
public sealed class ProjectionParentDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
    public List<ProjectionChildDto> Children { get; set; } = new List<ProjectionChildDto>();
}
public sealed class ProjectionChildDto
{
    public string Name { get; set; } = "";
    public List<ProjectionLeafDto> Leaves { get; set; } = new List<ProjectionLeafDto>();
}
public sealed class ProjectionLeafDto { public string Value { get; set; } = ""; }

/// <summary>Materializes constructed query expressions in memory; does not mock DTOs or expression building.</summary>
public sealed class MemoryProjectionExecutor : IProjectionQueryExecutor
{
    public List<string> Queries { get; } = new List<string>();
    public List<Expression> Expressions { get; } = new List<Expression>();
    public Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Queries.Add(query.Expression.ToString());
        Expressions.Add(query.Expression);
        return Task.FromResult<IReadOnlyList<T>>(query.ToList());
    }
}
