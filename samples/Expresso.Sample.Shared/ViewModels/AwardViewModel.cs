namespace Expresso.Sample.Shared.ViewModels;

/// <summary>Award returned by the sample API.</summary>
public sealed class AwardViewModel
{
    /// <summary>Award title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Calendar year the award was granted.</summary>
    public short Year { get; init; }
}
