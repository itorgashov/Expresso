using System;
using Expresso.Core.Filtering;

namespace Expresso.Sample.Shared.Filtering;

public sealed class RequestFieldsInfoProvider : IRequestFieldsInfoProvider
{
    private static readonly (string, Type)[] BookFields =
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
    };

    private static readonly (string, Type)[] PublisherFields =
    {
        ("name", typeof(string)),
        ("country", typeof(string)),
        ("location", typeof(string)),
    };

    public (string, Type)[] GetValidFilterFields(string context) => GetFields(context);

    public (string, Type)[] GetValidSortFields(string context) => GetFields(context);

    private static (string, Type)[] GetFields(string context) =>
        context.ToLowerInvariant() switch
        {
            "book" => BookFields,
            "author" => AuthorFields,
            "publisher" => PublisherFields,
            _ => Array.Empty<(string, Type)>(),
        };
}
