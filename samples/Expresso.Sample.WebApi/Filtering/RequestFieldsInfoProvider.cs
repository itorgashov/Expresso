using Expresso.Core.Filtering;

namespace Expresso.Sample.WebApi.Filtering;

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
        ("dateofbirth", typeof(DateOnly)),
        ("createdat", typeof(DateTime)),
    };

    private static readonly (string, Type)[] PublisherFields =
    {
        ("name", typeof(string)),
        ("country", typeof(string)),
        ("location", typeof(string)),
        ("opens", typeof(TimeOnly)),
        ("closes", typeof(TimeOnly)),
    };

    private static readonly CollectionModel[] BookAuthorCollections =
    {
        new CollectionModel("authors", new QueryModel(AuthorFields)),
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
            "author" => new QueryModel(AuthorFields),
            "publisher" => new QueryModel(PublisherFields),
            _ => QueryModel.Empty,
        };

    public QueryModel GetSortModel(string context) =>
        context.ToLowerInvariant() switch
        {
            "book" => new QueryModel(BookSortFields, BookAuthorCollections),
            "author" => new QueryModel(AuthorFields),
            "publisher" => new QueryModel(PublisherFields),
            _ => QueryModel.Empty,
        };
}
