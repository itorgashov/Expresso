using Expresso.Core.Filtering;

namespace Expresso.Sample.WebApi.Filtering;

/// <summary>
/// Field catalog for the net10 sample. Book and author queries include nested collections.
/// <c>dateofbirth</c> is <see cref="DateOnly"/> and publisher open/close times are <see cref="TimeOnly"/>.
/// </summary>
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
        ("opens", typeof(TimeOnly)),
        ("closes", typeof(TimeOnly)),
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

    /// <summary>Returns the scalar filter fields for <paramref name="context"/>.</summary>
    /// <param name="context"><c>book</c>, <c>author</c>, or <c>publisher</c>.</param>
    /// <returns>The allow-list, or an empty array for an unknown context.</returns>
    public (string, Type)[] GetValidFilterFields(string context) =>
        context.ToLowerInvariant() switch
        {
            "book" => BookFilterFields,
            "author" => AuthorFields,
            "publisher" => PublisherFields,
            _ => Array.Empty<(string, Type)>(),
        };

    /// <summary>Returns the scalar sort fields for <paramref name="context"/>. Book <c>externalid</c> is filter-only.</summary>
    /// <param name="context"><c>book</c>, <c>author</c>, or <c>publisher</c>.</param>
    /// <returns>The allow-list, or an empty array for an unknown context.</returns>
    public (string, Type)[] GetValidSortFields(string context) =>
        context.ToLowerInvariant() switch
        {
            "book" => BookSortFields,
            "author" => AuthorFields,
            "publisher" => PublisherFields,
            _ => Array.Empty<(string, Type)>(),
        };

    /// <summary>Returns the filter model, including <c>authors</c> and <c>awards</c> where the context has them.</summary>
    /// <param name="context"><c>book</c>, <c>author</c>, or <c>publisher</c>.</param>
    /// <returns>The model, or <see cref="QueryModel.Empty"/> for an unknown context.</returns>
    public QueryModel GetFilterModel(string context) =>
        context.ToLowerInvariant() switch
        {
            "book" => new QueryModel(BookFilterFields, BookAuthorCollections),
            "author" => new QueryModel(AuthorFields, AuthorAwardCollections),
            "publisher" => new QueryModel(PublisherFields),
            _ => QueryModel.Empty,
        };

    /// <summary>Returns the sort model, including nested <c>sortfor</c> collections where the context has them.</summary>
    /// <param name="context"><c>book</c>, <c>author</c>, or <c>publisher</c>.</param>
    /// <returns>The model, or <see cref="QueryModel.Empty"/> for an unknown context.</returns>
    public QueryModel GetSortModel(string context) =>
        context.ToLowerInvariant() switch
        {
            "book" => new QueryModel(BookSortFields, BookAuthorCollections),
            "author" => new QueryModel(AuthorFields, AuthorAwardCollections),
            "publisher" => new QueryModel(PublisherFields),
            _ => QueryModel.Empty,
        };
}
