using System;
using System.Collections.Generic;
using Expresso.SqlServer;

namespace Expresso.Sample.Shared.DataAccess;

internal static class SampleSqlMappings
{
    public static Dictionary<string, string> AuthorItemFields { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "firstname", "a.first_name" },
            { "lastname", "a.last_name" },
            { "displayname", "a.display_name" },
            { "dateofbirth", "a.date_of_birth" },
            { "createdat", "a.created_at" },
        };

    public static Dictionary<string, string> AwardItemFields { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "title", "aw.title" },
            { "year", "aw.year" },
        };

    public static CollectionSqlMapping AwardsOnAuthor { get; } = new CollectionSqlMapping(
        "awards",
        "dbo.award AS aw",
        "aw.author_id = a.id",
        AwardItemFields);

    public static CollectionSqlMapping BookAuthors { get; } = new CollectionSqlMapping(
        "authors",
        "dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id",
        "ba.book_id = b.id",
        AuthorItemFields,
        new[] { AwardsOnAuthor });
}
