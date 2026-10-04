namespace Expresso.Sample.WebApi.EfCore.Entities;

public sealed class Publisher
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string? Location { get; set; }

    public TimeOnly OpensAt { get; set; }

    public TimeOnly ClosesAt { get; set; }

    public List<Book> Books { get; set; } = new();
}
