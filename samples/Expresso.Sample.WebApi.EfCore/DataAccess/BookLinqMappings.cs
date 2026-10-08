using Expresso.Rendering.Linq;
using Expresso.Sample.WebApi.EfCore.Entities;

namespace Expresso.Sample.WebApi.EfCore.DataAccess;

internal static class BookLinqMappings
{
    public static readonly LinqQueryMapping<Award> Awards = new LinqQueryMapping<Award>()
        .Field("title", a => a.Title)
        .Field("year", a => (int)a.Year);

    public static readonly LinqQueryMapping<Author> Authors = new LinqQueryMapping<Author>()
        .Field("firstname", a => a.FirstName)
        .Field("lastname", a => a.LastName)
        .Field("displayname", a => a.DisplayName)
        .Field("dateofbirth", a => a.DateOfBirth)
        .Field("createdat", a => a.CreatedAt)
        .Field("id", a => a.Id)
        .Collection("awards", a => a.Awards, Awards);

    public static readonly LinqQueryMapping<Book> Books = new LinqQueryMapping<Book>()
        .Field("title", b => b.Title)
        .Field("year", b => (int)b.Year)
        .Field("isbn", b => b.Isbn)
        .Field("publisher", b => b.Publisher.Name)
        .Field("price", b => (double)b.Price)
        .Field("rating", b => b.Rating)
        .Field("createdat", b => b.CreatedAt)
        .Field("externalid", b => b.ExternalId)
        .Field("id", b => b.Id)
        .Collection("authors", b => b.Authors, Authors);

    public static readonly LinqQueryMapping<Publisher> Publishers = new LinqQueryMapping<Publisher>()
        .Field("name", p => p.Name)
        .Field("country", p => p.Country)
        .Field("location", p => p.Location)
        .Field("opens", p => p.OpensAt)
        .Field("closes", p => p.ClosesAt)
        .Field("id", p => p.Id);
}
