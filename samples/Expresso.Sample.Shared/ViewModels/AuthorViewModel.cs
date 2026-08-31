using System;
using System.Collections.Generic;

namespace Expresso.Sample.Shared.ViewModels;

public sealed class AuthorViewModel
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public DateTime? DateOfBirth { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<AwardViewModel> Awards { get; init; } = new List<AwardViewModel>();
}
