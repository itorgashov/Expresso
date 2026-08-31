using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Sorting;
using Expresso.Sample.Shared.Models;
using Expresso.SqlServer;
using Microsoft.Data.SqlClient;

namespace Expresso.Sample.Shared.DataAccess;

internal static class BookChildLoader
{
    private const string AuthorOrderParamPrefix = "authorOrder";
    private const string AwardOrderParamPrefix = "awardOrder";

    public static async Task LoadAuthorsAndAwardsAsync(
        SqlConnection connection,
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

        var authorSort = NestedSortHelper.ResolveNested(sortDirective, "authors");
        var awardSort = NestedSortHelper.ResolveNested(sortDirective, "authors", "awards");
        var authorOrderBy = NestedSortHelper.RenderOrderByOrDefault(
            authorSort,
            "a.display_name",
            SampleSqlMappings.AuthorItemFields,
            transformer,
            AuthorOrderParamPrefix,
            parameters: null);
        var awardOrderBy = NestedSortHelper.RenderOrderByOrDefault(
            awardSort,
            "aw.year, aw.title",
            SampleSqlMappings.AwardItemFields,
            transformer,
            AwardOrderParamPrefix,
            parameters: null);

        var idParameters = string.Join(", ", bookIds.Select((_, i) => $"@bookId{i}"));
        var authorSql =
            "SELECT ba.book_id, a.id, a.first_name, a.last_name, a.display_name, a.date_of_birth, a.created_at" +
            " FROM dbo.book_author AS ba" +
            " INNER JOIN dbo.author AS a ON a.id = ba.author_id" +
            $" WHERE ba.book_id IN ({idParameters})" +
            $" ORDER BY ba.book_id, {authorOrderBy}";

        var authorIds = new HashSet<int>();
        using (var command = new SqlCommand(authorSql, connection))
        {
            for (var i = 0; i < bookIds.Count; i++)
            {
                command.Parameters.AddWithValue($"@bookId{i}", bookIds[i]);
            }

            using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var bookId = reader.GetInt32(0);
                    var author = ReadAuthor(reader, startIndex: 1);
                    authorsByBookId[bookId].Add(author);
                    authorIds.Add(author.Id);
                }
            }
        }

        var awardsByAuthorId = await LoadAwardsAsync(
            connection,
            authorIds,
            awardOrderBy,
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
        SqlConnection connection,
        IReadOnlyCollection<int> authorIds,
        string orderBy,
        CancellationToken cancellationToken)
    {
        var awardsByAuthorId = new Dictionary<int, List<Award>>();
        if (authorIds.Count == 0)
        {
            return awardsByAuthorId;
        }

        var idList = authorIds.ToList();
        var idParameters = string.Join(", ", idList.Select((_, i) => $"@authorId{i}"));
        var sql =
            "SELECT aw.author_id, aw.title, aw.year" +
            " FROM dbo.award AS aw" +
            $" WHERE aw.author_id IN ({idParameters})" +
            $" ORDER BY aw.author_id, {orderBy}";

        using (var command = new SqlCommand(sql, connection))
        {
            for (var i = 0; i < idList.Count; i++)
            {
                command.Parameters.AddWithValue($"@authorId{i}", idList[i]);
            }

            using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var authorId = reader.GetInt32(0);
                    if (!awardsByAuthorId.TryGetValue(authorId, out var awards))
                    {
                        awards = new List<Award>();
                        awardsByAuthorId[authorId] = awards;
                    }

                    awards.Add(new Award
                    {
                        Title = reader.GetString(1),
                        Year = reader.GetInt16(2),
                    });
                }
            }
        }

        return awardsByAuthorId;
    }

    private static Author ReadAuthor(SqlDataReader reader, int startIndex) =>
        new Author
        {
            Id = reader.GetInt32(startIndex),
            FirstName = reader.GetString(startIndex + 1),
            LastName = reader.GetString(startIndex + 2),
            DisplayName = reader.GetString(startIndex + 3),
            DateOfBirth = reader.IsDBNull(startIndex + 4) ? null : reader.GetDateTime(startIndex + 4),
            CreatedAt = reader.GetDateTime(startIndex + 5),
        };
}
