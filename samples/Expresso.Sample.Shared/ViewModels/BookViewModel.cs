using System;
using System.Collections.Generic;

namespace Expresso.Sample.Shared.ViewModels;

/// <summary>Book returned by the sample API.</summary>
public sealed class BookViewModel
{
    /// <summary>Primary key.</summary>
    public int Id { get; init; }

    /// <summary>Book title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Publication year.</summary>
    public short Year { get; init; }

    /// <summary>ISBN, when the catalog has one.</summary>
    public string? Isbn { get; init; }

    /// <summary>List price.</summary>
    public decimal Price { get; init; }

    /// <summary>Average rating.</summary>
    public double Rating { get; init; }

    /// <summary>When the book row was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Stable identifier from the source catalog.</summary>
    public Guid ExternalId { get; init; }

    /// <summary>Name of the book's publisher.</summary>
    public string Publisher { get; init; } = string.Empty;

    /// <summary>Authors of the book, in the order loaded for the request.</summary>
    public List<AuthorViewModel> Authors { get; init; } = new List<AuthorViewModel>();
}
