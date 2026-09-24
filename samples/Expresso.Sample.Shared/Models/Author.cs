using System;
using System.Collections.Generic;

namespace Expresso.Sample.Shared.Models;

/// <summary>An author row, including awards loaded for that author.</summary>
public sealed class Author
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

    /// <summary>Awards loaded for this author.</summary>
    public List<Award> Awards { get; init; } = new List<Award>();
}
