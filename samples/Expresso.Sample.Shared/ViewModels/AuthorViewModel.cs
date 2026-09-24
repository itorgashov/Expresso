using System;
using System.Collections.Generic;

namespace Expresso.Sample.Shared.ViewModels;

/// <summary>Author returned by the sample API.</summary>
public sealed class AuthorViewModel
{
    /// <summary>Primary key.</summary>
    public int Id { get; init; }

    /// <summary>Given name.</summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>Family name.</summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>Name shown in the catalog.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Date of birth, when known.</summary>
    public DateTime? DateOfBirth { get; init; }

    /// <summary>When the author row was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Awards included with this author.</summary>
    public List<AwardViewModel> Awards { get; init; } = new List<AwardViewModel>();
}
