using System;
using System.Collections.Generic;
using System.Linq;
using Expresso.Sample.Shared.Models;

namespace Expresso.Sample.Shared.ViewModels;

/// <summary>Maps sample domain rows to API view models.</summary>
public static class ViewModelMapper
{
    /// <summary>Maps a book and its authors to the API shape.</summary>
    /// <param name="book">Book loaded by a repository.</param>
    /// <returns>The book view model.</returns>
    public static BookViewModel ToViewModel(Book book) =>
        new BookViewModel
        {
            Id = book.Id,
            Title = book.Title,
            Year = book.Year,
            Isbn = book.Isbn,
            Price = book.Price,
            Rating = book.Rating,
            CreatedAt = book.CreatedAt,
            ExternalId = book.ExternalId,
            Publisher = book.Publisher,
            Authors = book.Authors.Select(ToViewModel).ToList(),
        };

    /// <summary>Maps an author and that author's awards to the API shape.</summary>
    /// <param name="author">Author loaded by a repository.</param>
    /// <returns>The author view model.</returns>
    public static AuthorViewModel ToViewModel(Author author) =>
        new AuthorViewModel
        {
            Id = author.Id,
            FirstName = author.FirstName,
            LastName = author.LastName,
            DisplayName = author.DisplayName,
            DateOfBirth = author.DateOfBirth,
            CreatedAt = author.CreatedAt,
            Awards = author.Awards.Select(ToViewModel).ToList(),
        };

    /// <summary>Maps an award to the API shape.</summary>
    /// <param name="award">Award loaded with an author.</param>
    /// <returns>The award view model.</returns>
    public static AwardViewModel ToViewModel(Award award) =>
        new AwardViewModel
        {
            Title = award.Title,
            Year = award.Year,
        };

    /// <summary>Maps a publisher to the API shape.</summary>
    /// <param name="publisher">Publisher loaded by a repository.</param>
    /// <returns>The publisher view model.</returns>
    public static PublisherViewModel ToViewModel(Publisher publisher) =>
        new PublisherViewModel
        {
            Id = publisher.Id,
            Name = publisher.Name,
            Country = publisher.Country,
            Location = publisher.Location,
            OpensAt = publisher.OpensAt,
            ClosesAt = publisher.ClosesAt,
        };
}
