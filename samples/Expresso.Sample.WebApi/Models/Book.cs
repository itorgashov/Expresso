namespace Expresso.Sample.WebApi.Models;

public sealed class Book
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public short Year { get; init; }
    public string? Isbn { get; init; }
    public decimal Price { get; init; }
    public double Rating { get; init; }
    public DateTime CreatedAt { get; init; }
    public string Publisher { get; init; } = string.Empty;
    public List<string> Authors { get; init; } = [];
}
