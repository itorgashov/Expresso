using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Rendering;

namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Public response fields and internal identity for the ADO.NET sample repositories.</summary>
public sealed class SampleProjectionMappings
{
    /// <summary>Creates mappings for the selected SQL engine.</summary>
    public SampleProjectionMappings(ISampleSql sql)
    {
        var query = new SampleSqlMappings(sql);
        Awards = new SqlProjectionMapping().Key("id", sql.Col("aw", "id"), typeof(int))
            .Field("title", "title", sql.Col("aw", "title"), typeof(string))
            .Field("year", "year", sql.Col("aw", "year"), typeof(short));
        Authors = new SqlProjectionMapping().Key("id", sql.Col("a", "id"), typeof(int))
            .Field("id", "id", sql.Col("a", "id"), typeof(int))
            .Field("firstname", "firstName", sql.Col("a", "first_name"), typeof(string))
            .Field("lastname", "lastName", sql.Col("a", "last_name"), typeof(string))
            .Field("displayname", "displayName", sql.Col("a", "display_name"), typeof(string))
            .Field("dateofbirth", "dateOfBirth", sql.Col("a", "date_of_birth"), typeof(DateTime?))
            .Field("createdat", "createdAt", sql.Col("a", "created_at"), typeof(DateTime))
            .Collection("awards", "awards", new SqlCollectionProjectionMapping(sql.TableAs("award", "aw"), Awards,
                new[] { new SqlProjectionKey("id", sql.Col("aw", "author_id"), typeof(int)) },
                sql.Col("aw", "year") + ", " + sql.Col("aw", "title") + ", " + sql.Col("aw", "id"),
                new SqlQueryMapping(query.AwardItemFields)));
        Books = new SqlProjectionMapping().Key("id", sql.Col("b", "id"), typeof(int))
            .Field("id", "id", sql.Col("b", "id"), typeof(int))
            .Field("title", "title", sql.Col("b", "title"), typeof(string))
            .Field("year", "year", sql.Col("b", "year"), typeof(short))
            .Field("isbn", "isbn", sql.Col("b", "isbn"), typeof(string))
            .Field("price", "price", sql.Col("b", "price"), typeof(decimal))
            .Field("rating", "rating", sql.Col("b", "rating"), typeof(double))
            .Field("createdat", "createdAt", sql.Col("b", "created_at"), typeof(DateTime))
            .Field("externalid", "externalId", sql.Col("b", "external_id"), typeof(Guid))
            .Field("publisher", "publisher", sql.Col("p", "name"), typeof(string))
            .Collection("authors", "authors", new SqlCollectionProjectionMapping(
                sql.TableAs("book_author", "ba") + " INNER JOIN " + sql.TableAs("author", "a") +
                " ON " + sql.Col("a", "id") + " = " + sql.Col("ba", "author_id"), Authors,
                new[] { new SqlProjectionKey("id", sql.Col("ba", "book_id"), typeof(int)) },
                sql.Col("a", "display_name") + ", " + sql.Col("a", "id"),
                new SqlQueryMapping(query.AuthorItemFields, new[] { query.AwardsOnAuthor })));
        Publishers = new SqlProjectionMapping().Key("id", sql.Col("p", "id"), typeof(int))
            .Field("id", "id", sql.Col("p", "id"), typeof(int))
            .Field("name", "name", sql.Col("p", "name"), typeof(string))
            .Field("country", "country", sql.Col("p", "country"), typeof(string))
            .Field("location", "location", sql.Col("p", "location"), typeof(string))
            .Field("opens", "opensAt", sql.Col("p", "opens_at"), typeof(TimeSpan))
            .Field("closes", "closesAt", sql.Col("p", "closes_at"), typeof(TimeSpan));
    }
    /// <summary>Book projection.</summary>
    public SqlProjectionMapping Books { get; }
    /// <summary>Author projection.</summary>
    public SqlProjectionMapping Authors { get; }
    /// <summary>Award projection.</summary>
    public SqlProjectionMapping Awards { get; }
    /// <summary>Publisher projection.</summary>
    public SqlProjectionMapping Publishers { get; }
}
