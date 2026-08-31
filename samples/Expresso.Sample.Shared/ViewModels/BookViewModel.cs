using System;
using System.Collections.Generic;

namespace Expresso.Sample.Shared.ViewModels;

public sealed class BookViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public short Year { get; init; }
    public string? Isbn { get; init; }
    public decimal Price { get; init; }
    public double Rating { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid ExternalId { get; init; }
    public string Publisher { get; init; } = string.Empty;
    public List<AuthorViewModel> Authors { get; init; } = new List<AuthorViewModel>();
}
