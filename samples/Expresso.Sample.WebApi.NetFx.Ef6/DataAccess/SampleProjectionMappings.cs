using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Sorting;
using Expresso.Rendering.Linq;
using Expresso.Sample.WebApi.NetFx.Ef6.Entities;
using Expresso.Sample.WebApi.NetFx.Ef6.ViewModels;

namespace Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

/// <summary>Typed response mappings and internal identity for conditional child execution.</summary>
public static class SampleProjectionMappings
{
    /// <summary>Cached awards response mapping.</summary>
    public static readonly LinqProjectionMapping<Award, AwardViewModel> Awards = new LinqProjectionMapping<Award, AwardViewModel>()
        .Key("id", a => a.Id).Field("title", "title", a => a.Title, a => a.Title).Field("year", "year", a => a.Year, a => a.Year);
    /// <summary>Cached authors response mapping.</summary>
    public static readonly LinqProjectionMapping<Author, AuthorViewModel> Authors = new LinqProjectionMapping<Author, AuthorViewModel>()
        .Key("id", a => a.Id).Field("id", "id", a => a.Id, a => a.Id)
        .Field("firstname", "firstName", a => a.FirstName, a => a.FirstName)
        .Field("lastname", "lastName", a => a.LastName, a => a.LastName)
        .Field("displayname", "displayName", a => a.DisplayName, a => a.DisplayName)
        .Field("dateofbirth", "dateOfBirth", a => a.DateOfBirth, a => a.DateOfBirth)
        .Field("createdat", "createdAt", a => a.CreatedAt, a => a.CreatedAt)
        .Collection("awards", "awards", a => a.Awards, a => a.Awards, Awards, BookLinqMappings.Awards,
            Sort("year", typeof(int)).ThenBy(new Field("title", typeof(string))).ThenBy(new Field("id", typeof(int))));
    /// <summary>Cached books response mapping.</summary>
    public static readonly LinqProjectionMapping<Book, BookViewModel> Books = new LinqProjectionMapping<Book, BookViewModel>()
        .Key("id", b => b.Id).Field("id", "id", b => b.Id, b => b.Id)
        .Field("title", "title", b => b.Title, b => b.Title).Field("year", "year", b => b.Year, b => b.Year)
        .Field("isbn", "isbn", b => b.Isbn, b => b.Isbn).Field("price", "price", b => b.Price, b => b.Price)
        .Field("rating", "rating", b => b.Rating, b => b.Rating)
        .Field("createdat", "createdAt", b => b.CreatedAt, b => b.CreatedAt)
        .Field("externalid", "externalId", b => b.ExternalId, b => b.ExternalId)
        .Field("publisher", "publisher", b => b.Publisher.Name, b => b.Publisher)
        .Collection("authors", "authors", b => b.Authors, b => b.Authors, Authors, BookLinqMappings.Authors,
            Sort("displayname", typeof(string)).ThenBy(new Field("id", typeof(int))));

    private static SortDirective Sort(string name, Type type) => new SortDirective(new[] {
        new SortDirectiveItem { Expression = new Field(name, type), Direction = SortDirection.Ascending } });
}
