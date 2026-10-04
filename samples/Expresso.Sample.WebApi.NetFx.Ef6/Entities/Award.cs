namespace Expresso.Sample.WebApi.NetFx.Ef6.Entities;

public sealed class Award
{
    public int Id { get; set; }

    public int AuthorId { get; set; }

    public Author Author { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public short Year { get; set; }
}
