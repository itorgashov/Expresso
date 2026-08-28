using System;

namespace Expresso.Sample.Shared.ViewModels;

public sealed class PublisherViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string? Location { get; init; }
    public TimeSpan OpensAt { get; init; }
    public TimeSpan ClosesAt { get; init; }
}
