namespace Expresso.Sample.WebApi.Models;

public sealed class Publisher
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string? Location { get; init; }
}
