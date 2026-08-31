using System;
using System.Collections.Generic;
using System.Linq;
using Expresso.Sample.Shared.Models;

namespace Expresso.Sample.Shared.ViewModels;

public static class ViewModelMapper
{
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

    public static AwardViewModel ToViewModel(Award award) =>
        new AwardViewModel
        {
            Title = award.Title,
            Year = award.Year,
        };

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
