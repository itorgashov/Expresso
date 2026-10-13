using Expresso.Core.Projection;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.TestCases;

/// <summary>A composite-key, many-to-many graph with distinct nullable leaf identities.</summary>
public static class SharedProjectionFixture
{
    public static List<SharedBook> Data(bool incompleteKeys)
    {
        var author = new SharedAuthor { Tenant = 1, Id = 1, Name = "shared", Awards = new List<SharedAward> {
            new SharedAward { Tenant = 1, Id = 1, AuthorId = 1, Identity = incompleteKeys ? null : 1, Title = "prize" },
            new SharedAward { Tenant = 1, Id = 2, AuthorId = 1, Identity = incompleteKeys ? null : 2, Title = "prize" } } };
        var foreign = new SharedAuthor { Tenant = 2, Id = 1, Name = "other", Awards = new List<SharedAward> {
            new SharedAward { Tenant = 2, Id = 1, AuthorId = 1, Identity = 1, Title = "foreign" } } };
        return Enumerable.Range(1, 10).Select(id => new SharedBook { Tenant = 1, Id = id,
            Title = "B" + id, Authors = new List<SharedAuthor> { author } })
            .Concat(new[] { new SharedBook { Tenant = 2, Id = 1, Authors = new List<SharedAuthor> { foreign } } }).ToList();
    }

    public static ProjectionDirective Nested() => new(new[] { new ProjectionSelection("title"),
        new ProjectionSelection("authors", new[] { new ProjectionSelection("name"),
            new ProjectionSelection("awards", new[] { new ProjectionSelection("title") }) }) });

    public static LinqProjectionMapping<SharedBook, SharedBookDto> Mapping()
    {
        var awards = new LinqProjectionMapping<SharedAward, SharedAwardDto>()
            .Key("tenant", x => x.Tenant).Key("identity", x => x.Identity)
            .Field("title", "title", x => x.Title, x => x.Title);
        var authors = new LinqProjectionMapping<SharedAuthor, SharedAuthorDto>()
            .Key("tenant", x => x.Tenant).Key("id", x => x.Id)
            .Field("name", "name", x => x.Name, x => x.Name)
            .Collection("awards", "awards", x => x.Awards, x => x.Awards, awards, new LinqQueryMapping<SharedAward>());
        return new LinqProjectionMapping<SharedBook, SharedBookDto>()
            .Key("tenant", x => x.Tenant).Key("id", x => x.Id)
            .Field("title", "title", x => x.Title, x => x.Title)
            .Collection("authors", "authors", x => x.Authors, x => x.Authors, authors, new LinqQueryMapping<SharedAuthor>());
    }
}

public sealed class SharedBook
{
    public int Tenant { get; set; }
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public ICollection<SharedAuthor> Authors { get; set; } = new List<SharedAuthor>();
}
public sealed class SharedAuthor
{
    public int Tenant { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Bio { get; set; }
    public ICollection<SharedAward> Awards { get; set; } = new List<SharedAward>();
}
public sealed class SharedAward
{
    public int Tenant { get; set; }
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public int? Identity { get; set; }
    public string Title { get; set; } = "";
}
public sealed class SharedBookDto
{
    public string Title { get; set; } = "";
    public ICollection<SharedAuthorDto> Authors { get; set; } = new List<SharedAuthorDto>();
}
public sealed class SharedAuthorDto
{
    public string Name { get; set; } = "";
    public ICollection<SharedAwardDto> Awards { get; set; } = new List<SharedAwardDto>();
}
public sealed class SharedAwardDto { public string Title { get; set; } = ""; }
