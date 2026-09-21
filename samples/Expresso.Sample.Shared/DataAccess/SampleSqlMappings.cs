using System;
using System.Collections.Generic;
using Expresso.Rendering;

namespace Expresso.Sample.Shared.DataAccess;

internal sealed class SampleSqlMappings
{
    public SampleSqlMappings(ISampleSql sql)
    {
        AuthorItemFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "firstname", sql.Col("a", "first_name") },
            { "lastname", sql.Col("a", "last_name") },
            { "displayname", sql.Col("a", "display_name") },
            { "dateofbirth", sql.Col("a", "date_of_birth") },
            { "createdat", sql.Col("a", "created_at") },
        };

        AwardItemFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "title", sql.Col("aw", "title") },
            { "year", sql.Col("aw", "year") },
        };

        AwardsOnAuthor = new CollectionSqlMapping(
            "awards",
            sql.TableAs("award", "aw"),
            sql.Col("aw", "author_id") + " = " + sql.Col("a", "id"),
            AwardItemFields);

        BookAuthors = new CollectionSqlMapping(
            "authors",
            sql.TableAs("book_author", "ba") + " INNER JOIN " + sql.TableAs("author", "a") +
            " ON " + sql.Col("a", "id") + " = " + sql.Col("ba", "author_id"),
            sql.Col("ba", "book_id") + " = " + sql.Col("b", "id"),
            AuthorItemFields,
            new[] { AwardsOnAuthor });
    }

    public CollectionSqlMapping AwardsOnAuthor { get; }

    public CollectionSqlMapping BookAuthors { get; }

    public Dictionary<string, string> AuthorItemFields { get; }

    public Dictionary<string, string> AwardItemFields { get; }
}
