namespace Expresso.Sample.WebApi.EfCore.Entities;

public sealed class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public short Year { get; set; }

    public string? Isbn { get; set; }

    public int PublisherId { get; set; }

    public Publisher Publisher { get; set; } = null!;

    public double Rating { get; set; }

    public decimal Price { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid ExternalId { get; set; }

    public List<Author> Authors { get; set; } = new();
}
