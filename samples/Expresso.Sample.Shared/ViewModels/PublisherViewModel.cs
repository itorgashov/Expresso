using System;

namespace Expresso.Sample.Shared.ViewModels;

/// <summary>Publisher returned by the sample API.</summary>
public sealed class PublisherViewModel
{
    /// <summary>Primary key.</summary>
    public int Id { get; init; }

    /// <summary>Publisher name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Country of the publisher.</summary>
    public string Country { get; init; } = string.Empty;

    /// <summary>City or office location, when known.</summary>
    public string? Location { get; init; }

    /// <summary>Local time of day when the office opens.</summary>
    public TimeSpan OpensAt { get; init; }

    /// <summary>Local time of day when the office closes.</summary>
    public TimeSpan ClosesAt { get; init; }
}
