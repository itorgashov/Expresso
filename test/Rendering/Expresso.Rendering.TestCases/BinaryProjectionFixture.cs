using Expresso.Rendering.Linq;

namespace Expresso.Rendering.TestCases;

/// <summary>Composite binary/integer identities with content-equal foreign keys stored in different arrays.</summary>
public static class BinaryProjectionFixture
{
    public static IQueryable<BinaryProjectionParent> Data() => new[] {
        new BinaryProjectionParent { Tenant = new byte[] { 1, 2 }, Id = 1, Children = new List<BinaryProjectionChild> {
            new BinaryProjectionChild { Id = 1, ParentTenant = new byte[] { 1, 2 }, ParentId = 1, Name = "first" } } },
        new BinaryProjectionParent { Tenant = new byte[] { 3 }, Id = 1, Children = new List<BinaryProjectionChild> {
            new BinaryProjectionChild { Id = 2, ParentTenant = new byte[] { 3 }, ParentId = 1, Name = "second" } } },
        new BinaryProjectionParent { Tenant = new byte[] { 4 }, Id = 1 } }.AsQueryable();

    public static LinqProjectionMapping<BinaryProjectionParent, BinaryProjectionDto> Mapping() =>
        new LinqProjectionMapping<BinaryProjectionParent, BinaryProjectionDto>().Key("tenant", x => x.Tenant).Key("id", x => x.Id)
            .Collection("children", "children", x => x.Children, x => x.Children,
                new LinqProjectionMapping<BinaryProjectionChild, ProjectionChildDto>().Key("id", x => x.Id)
                    .Field("name", "name", x => x.Name, x => x.Name),
                new LinqQueryMapping<BinaryProjectionChild>().Field("name", x => x.Name));
}

public sealed class BinaryProjectionParent
{
    public byte[] Tenant { get; set; } = Array.Empty<byte>();
    public int Id { get; set; }
    public List<BinaryProjectionChild> Children { get; set; } = new List<BinaryProjectionChild>();
}
public sealed class BinaryProjectionChild
{
    public int Id { get; set; }
    public byte[] ParentTenant { get; set; } = Array.Empty<byte>();
    public int ParentId { get; set; }
    public string Name { get; set; } = "";
}
public sealed class BinaryProjectionDto
{
    public List<ProjectionChildDto> Children { get; set; } = new List<ProjectionChildDto> { new ProjectionChildDto { Name = "constructor default" } };
}
