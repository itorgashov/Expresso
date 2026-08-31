using System;
using System.Collections.Generic;

namespace Expresso.Sample.Shared.Models;

public sealed class Author
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public DateTime? DateOfBirth { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<Award> Awards { get; init; } = new List<Award>();
}
