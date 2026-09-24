namespace Expresso.Sample.Shared.Models;

/// <summary>An award granted to an author.</summary>
public sealed class Award
{
    /// <summary>Award title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Calendar year the award was granted.</summary>
    public short Year { get; init; }
}
