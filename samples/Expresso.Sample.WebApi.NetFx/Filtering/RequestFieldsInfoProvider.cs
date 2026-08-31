using System;
using Expresso.Core.Filtering;

namespace Expresso.Sample.WebApi.NetFx.Filtering;

public sealed class RequestFieldsInfoProvider : IRequestFieldsInfoProvider, IRequestQueryModelProvider
{
    private static readonly (string, Type)[] BookFilterFields =
    {
        ("title", typeof(string)),
        ("year", typeof(int)),
        ("isbn", typeof(string)),
        ("publisher", typeof(string)),
        ("price", typeof(double)),
        ("rating", typeof(double)),
        ("createdat", typeof(DateTime)),
        ("externalid", typeof(Guid)),
    };

    private static readonly (string, Type)[] BookSortFields =
    {
        ("title", typeof(string)),
        ("year", typeof(int)),
        ("isbn", typeof(string)),
        ("publisher", typeof(string)),
        ("price", typeof(double)),
        ("rating", typeof(double)),
        ("createdat", typeof(DateTime)),
    };

    private static readonly (string, Type)[] AuthorFields =
    {
        ("firstname", typeof(string)),
        ("lastname", typeof(string)),
        ("displayname", typeof(string)),
        ("dateofbirth", typeof(DateTime)),
        ("createdat", typeof(DateTime)),
    };

    private static readonly (string, Type)[] AwardFields =
    {
        ("title", typeof(string)),
        ("year", typeof(int)),
    };

    private static readonly (string, Type)[] PublisherFields =
    {
        ("name", typeof(string)),
        ("country", typeof(string)),
        ("location", typeof(string)),
        ("opens", typeof(TimeSpan)),
        ("closes", typeof(TimeSpan)),
    };

    private static readonly CollectionModel[] AuthorAwardCollections =
    {
        new CollectionModel("awards", new QueryModel(AwardFields)),
    };

    private static readonly QueryModel AuthorItemsModel =
        new QueryModel(AuthorFields, AuthorAwardCollections);

    private static readonly CollectionModel[] BookAuthorCollections =
    {
        new CollectionModel("authors", AuthorItemsModel),
    };

    public (string, Type)[] GetValidFilterFields(string context) =>
        context.ToLowerInvariant() switch
        {
            "book" => BookFilterFields,
            "author" => AuthorFields,
            "publisher" => PublisherFields,
            _ => Array.Empty<(string, Type)>(),
        };

    public (string, Type)[] GetValidSortFields(string context) =>
        context.ToLowerInvariant() switch
        {
            "book" => BookSortFields,
            "author" => AuthorFields,
            "publisher" => PublisherFields,
            _ => Array.Empty<(string, Type)>(),
        };

    public QueryModel GetFilterModel(string context) =>
        context.ToLowerInvariant() switch
        {
            "book" => new QueryModel(BookFilterFields, BookAuthorCollections),
            "author" => new QueryModel(AuthorFields, AuthorAwardCollections),
            "publisher" => new QueryModel(PublisherFields),
            _ => QueryModel.Empty,
        };

    public QueryModel GetSortModel(string context) =>
        context.ToLowerInvariant() switch
        {
            "book" => new QueryModel(BookSortFields, BookAuthorCollections),
            "author" => new QueryModel(AuthorFields, AuthorAwardCollections),
            "publisher" => new QueryModel(PublisherFields),
            _ => QueryModel.Empty,
        };
}
