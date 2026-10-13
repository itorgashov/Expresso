using Expresso.Core.Projection;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.TestCases;

/// <summary>A three-level entity hierarchy with composite keys and a shared derived collection parent.</summary>
public static class InheritanceProjectionFixture
{
    public static IQueryable<InheritedBook> Data()
    {
        var writer = new ProjectionFeaturedWriter { Tenant = 1, Id = 1, Name = "chosen", Feature = "featured",
            Prizes = new List<InheritedPrize> {
                new InheritedPrize { Tenant = 1, Id = 1, WriterId = 1, Title = "prize" },
                new InheritedPrize { Tenant = 1, Id = 2, WriterId = 1, Title = "prize" } } };
        var foreign = new ProjectionWriter { Tenant = 2, Id = 1, Name = "foreign", Prizes = new List<InheritedPrize> {
            new InheritedPrize { Tenant = 2, Id = 1, WriterId = 1, Title = "foreign" } } };
        return new[] {
            new InheritedBook { Tenant = 1, Id = 1, Title = "first", Writers = new List<ProjectionWriter> { writer } },
            new InheritedBook { Tenant = 1, Id = 2, Title = "second", Writers = new List<ProjectionWriter> { writer } },
            new InheritedBook { Tenant = 1, Id = 3, Title = "empty" },
            new InheritedBook { Tenant = 2, Id = 1, Title = "foreign", Writers = new List<ProjectionWriter> { foreign } }
        }.AsQueryable();
    }

    public static LinqProjectionMapping<ProjectionWriter, SharedAuthorDto> WriterMapping() =>
        new LinqProjectionMapping<ProjectionWriter, SharedAuthorDto>()
            .Key("tenant", x => x.Tenant).Key("id", x => x.Id)
            .Field("name", "name", x => x.Name, x => x.Name)
            .Collection("prizes", "prizes", x => x.Prizes, x => x.Awards,
                new LinqProjectionMapping<InheritedPrize, SharedAwardDto>()
                    .Key("tenant", x => x.Tenant).Key("identity", x => x.Identity)
                    .Field("title", "title", x => x.Title, x => x.Title), new LinqQueryMapping<InheritedPrize>());

    public static LinqProjectionMapping<InheritedBook, InheritedBookDto> Mapping() =>
        new LinqProjectionMapping<InheritedBook, InheritedBookDto>()
            .Key("tenant", x => x.Tenant).Key("id", x => x.Id)
            .Field("title", "title", x => x.Title, x => x.Title)
            .Collection("writers", "writers", x => x.Writers, x => x.Writers, WriterMapping(), new LinqQueryMapping<ProjectionWriter>());

    public static ProjectionDirective Nested() => new(new[] { new ProjectionSelection("title"),
        new ProjectionSelection("writers", new[] { new ProjectionSelection("name"),
            new ProjectionSelection("prizes", new[] { new ProjectionSelection("title") }) }) });
}

public sealed class InheritedBook
{
    public int Tenant { get; set; }
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public ICollection<ProjectionWriter> Writers { get; set; } = new List<ProjectionWriter>();
}
public class ProjectionPerson
{
    public int Tenant { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Bio { get; set; }
}
public class ProjectionWriter : ProjectionPerson
{
    public ICollection<InheritedPrize> Prizes { get; set; } = new List<InheritedPrize>();
}
public sealed class ProjectionFeaturedWriter : ProjectionWriter
{
    public string Feature { get; set; } = "";
}
public sealed class InheritedPrize
{
    public int Tenant { get; set; }
    public int Id { get; set; }
    public int WriterId { get; set; }
    public int? Identity { get; set; }
    public string Title { get; set; } = "";
}
public sealed class InheritedBookDto
{
    public string Title { get; set; } = "";
    public ICollection<SharedAuthorDto> Writers { get; set; } = new List<SharedAuthorDto>();
}
