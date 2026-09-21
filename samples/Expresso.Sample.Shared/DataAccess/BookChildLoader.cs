using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Sorting;
using Expresso.Sample.Shared.Models;
using Expresso.Rendering;

namespace Expresso.Sample.Shared.DataAccess;

internal static class BookChildLoader
{
    private const string AuthorOrderParamPrefix = "authorOrder";
    private const string AwardOrderParamPrefix = "awardOrder";

    public static async Task LoadAuthorsAndAwardsAsync(
        DbConnection connection,
        ISampleDb db,
        IReadOnlyList<Book> books,
        SortDirective? sortDirective,
        IExpressionToQueryClauseTransformer transformer,
        CancellationToken cancellationToken)
    {
        if (books.Count == 0)
        {
            return;
        }

        var bookIds = books.Select(b => b.Id).Distinct().ToList();
        var authorsByBookId = books.ToDictionary(b => b.Id, _ => new List<Author>());

        var sql = db.Sql;
        var mappings = new SampleSqlMappings(sql);
        var authorParams = new Dictionary<string, object>();
        var awardParams = new Dictionary<string, object>();
        var authorSort = NestedSortHelper.ResolveNested(sortDirective, "authors");
        var awardSort = NestedSortHelper.ResolveNested(sortDirective, "authors", "awards");
        var authorOrderBy = NestedSortHelper.RenderOrderByOrDefault(
            authorSort,
            sql.Col("a", "display_name"),
            mappings.AuthorItemFields,
            transformer,
            AuthorOrderParamPrefix,
            authorParams);
        var awardOrderBy = NestedSortHelper.RenderOrderByOrDefault(
            awardSort,
            sql.Col("aw", "year") + ", " + sql.Col("aw", "title"),
            mappings.AwardItemFields,
            transformer,
            AwardOrderParamPrefix,
            awardParams);

        var idParameters = string.Join(", ", bookIds.Select((_, i) => sql.Param("bookId" + i)));
        var authorSql =
            "SELECT " + sql.Col("ba", "book_id") + ", " + sql.Col("a", "id") + ", " + sql.Col("a", "first_name") + ", " +
            sql.Col("a", "last_name") + ", " + sql.Col("a", "display_name") + ", " + sql.Col("a", "date_of_birth") + ", " +
            sql.Col("a", "created_at") +
            " FROM " + sql.TableAs("book_author", "ba") +
            " INNER JOIN " + sql.TableAs("author", "a") + " ON " + sql.Col("a", "id") + " = " + sql.Col("ba", "author_id") +
            " WHERE " + sql.Col("ba", "book_id") + " IN (" + idParameters + ")" +
            " ORDER BY " + sql.Col("ba", "book_id") + ", " + authorOrderBy;

        var authorIds = new HashSet<int>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = authorSql;
            for (var i = 0; i < bookIds.Count; i++)
            {
                db.Bind(command, db.Sql.Param("bookId" + i), bookIds[i]);
            }

            db.BindAll(command, authorParams);

            using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var bookId = SampleDbValues.GetInt32(reader, 0);
                    var author = ReadAuthor(reader, startIndex: 1);
                    authorsByBookId[bookId].Add(author);
                    authorIds.Add(author.Id);
                }
            }
        }

        var awardsByAuthorId = await LoadAwardsAsync(
            connection,
            db,
            authorIds,
            awardOrderBy,
            awardParams,
            cancellationToken);

        foreach (var book in books)
        {
            foreach (var author in authorsByBookId[book.Id])
            {
                if (awardsByAuthorId.TryGetValue(author.Id, out var awards))
                {
                    author.Awards.AddRange(awards);
                }

                book.Authors.Add(author);
            }
        }
    }

    private static async Task<Dictionary<int, List<Award>>> LoadAwardsAsync(
        DbConnection connection,
        ISampleDb db,
        IReadOnlyCollection<int> authorIds,
        string orderBy,
        Dictionary<string, object> awardParams,
        CancellationToken cancellationToken)
    {
        var awardsByAuthorId = new Dictionary<int, List<Award>>();
        if (authorIds.Count == 0)
        {
            return awardsByAuthorId;
        }

        var idList = authorIds.ToList();
        var idParameters = string.Join(", ", idList.Select((_, i) => db.Sql.Param("authorId" + i)));
        var sqlCatalog = db.Sql;
        var sql =
            "SELECT " + sqlCatalog.Col("aw", "author_id") + ", " + sqlCatalog.Col("aw", "title") + ", " + sqlCatalog.Col("aw", "year") +
            " FROM " + sqlCatalog.TableAs("award", "aw") +
            " WHERE " + sqlCatalog.Col("aw", "author_id") + " IN (" + idParameters + ")" +
            " ORDER BY " + sqlCatalog.Col("aw", "author_id") + ", " + orderBy;

        using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;
            for (var i = 0; i < idList.Count; i++)
            {
                db.Bind(command, db.Sql.Param("authorId" + i), idList[i]);
            }

            db.BindAll(command, awardParams);

            using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var authorId = SampleDbValues.GetInt32(reader, 0);
                    if (!awardsByAuthorId.TryGetValue(authorId, out var awards))
                    {
                        awards = new List<Award>();
                        awardsByAuthorId[authorId] = awards;
                    }

                    awards.Add(new Award
                    {
                        Title = reader.GetString(1),
                        Year = SampleDbValues.GetInt16(reader, 2),
                    });
                }
            }
        }

        return awardsByAuthorId;
    }

    private static Author ReadAuthor(DbDataReader reader, int startIndex) =>
        new Author
        {
            Id = SampleDbValues.GetInt32(reader, startIndex),
            FirstName = reader.GetString(startIndex + 1),
            LastName = reader.GetString(startIndex + 2),
            DisplayName = reader.GetString(startIndex + 3),
            DateOfBirth = reader.IsDBNull(startIndex + 4) ? null : SampleDbValues.GetDateTime(reader, startIndex + 4),
            CreatedAt = SampleDbValues.GetDateTime(reader, startIndex + 5),
        };
}
