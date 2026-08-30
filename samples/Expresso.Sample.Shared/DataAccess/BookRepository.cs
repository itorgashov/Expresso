using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Sample.Shared.Models;
using Expresso.SqlServer;
using Microsoft.Data.SqlClient;

namespace Expresso.Sample.Shared.DataAccess;

public sealed class BookRepository : IRepository<Book>
{
    private const string WhereParamPrefix = "wparam";
    private const string OrderParamPrefix = "oparam";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IExpressionToQueryClauseTransformer _criteriaTransformer;

    private readonly SqlQueryMapping _queryMapping = new SqlQueryMapping(
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "title", "b.title" },
            { "year", "b.year" },
            { "isbn", "b.isbn" },
            { "publisher", "p.name" },
            { "price", "b.price" },
            { "rating", "b.rating" },
            { "createdat", "b.created_at" },
            { "externalid", "b.external_id" },
        },
        new[]
        {
            new CollectionSqlMapping(
                "authors",
                "dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id",
                "ba.book_id = b.id",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "firstname", "a.first_name" },
                    { "lastname", "a.last_name" },
                    { "displayname", "a.display_name" },
                    { "dateofbirth", "a.date_of_birth" },
                    { "createdat", "a.created_at" },
                }),
        });

    private const string BaseSelect =
        "SELECT" +
        " b.id," +
        " b.title," +
        " b.year," +
        " b.isbn," +
        " b.price," +
        " b.rating," +
        " b.created_at," +
        " b.external_id," +
        " p.name AS publisher_name" +
        " FROM dbo.book AS b" +
        " INNER JOIN dbo.publisher AS p ON p.id = b.publisher_id";

    public BookRepository(
        ISqlConnectionFactory connectionFactory,
        IExpressionToQueryClauseTransformer criteriaTransformer)
    {
        _connectionFactory = connectionFactory;
        _criteriaTransformer = criteriaTransformer;
    }

    public async Task<IReadOnlyList<Book>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using (connection)
        {
            var (sql, parameters) = BuildSelectQuery(filterCriteria, sortDirective);

            var books = new List<Book>();
            using (var command = new SqlCommand(sql, connection))
            {
                command.AddParameters(parameters);
                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        books.Add(ReadBook(reader));
                    }
                }
            }

            await LoadAuthorsAsync(connection, books, cancellationToken);
            return books;
        }
    }

    public async Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using (connection)
        {
            var sql = BaseSelect + " WHERE b.id = @id";

            Book? book = null;
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", id);
                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        book = ReadBook(reader);
                    }
                }
            }

            if (book is null)
            {
                return null;
            }

            await LoadAuthorsAsync(connection, new List<Book> { book }, cancellationToken);
            return book;
        }
    }

    private (string sql, Dictionary<string, object>? parameters) BuildSelectQuery(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective)
    {
        var sql = new StringBuilder(BaseSelect);
        Dictionary<string, object>? parameters = null;

        if (filterCriteria is not null)
        {
            var result = _criteriaTransformer.RenderWhereClause(filterCriteria, _queryMapping, WhereParamPrefix);
            sql.Append(" WHERE ");
            sql.Append(result.whereClause);
            parameters = new Dictionary<string, object>(result.parameters);
        }

        if (sortDirective is not null)
        {
            var result = _criteriaTransformer.RenderOrderByClause(sortDirective, _queryMapping, OrderParamPrefix);
            sql.Append(" ORDER BY ");
            sql.Append(result.orderByClause);
            parameters ??= new Dictionary<string, object>();
            SqlParameterExtensions.MergeParameters(parameters, result.parameters);
        }

        return (sql.ToString(), parameters);
    }

    private static Book ReadBook(SqlDataReader reader) =>
        new Book
        {
            Id = reader.GetInt32(0),
            Title = reader.GetString(1),
            Year = reader.GetInt16(2),
            Isbn = reader.IsDBNull(3) ? null : reader.GetString(3),
            Price = reader.GetDecimal(4),
            Rating = reader.GetDouble(5),
            CreatedAt = reader.GetDateTime(6),
            ExternalId = reader.GetGuid(7),
            Publisher = reader.GetString(8),
        };

    private static async Task LoadAuthorsAsync(
        SqlConnection connection,
        IReadOnlyList<Book> books,
        CancellationToken cancellationToken)
    {
        if (books.Count == 0)
        {
            return;
        }

        var bookIds = books.Select(b => b.Id).Distinct().ToList();
        var authorsByBookId = books.ToDictionary(b => b.Id, _ => new List<string>());

        var idParameters = string.Join(", ", bookIds.Select((_, i) => $"@bookId{i}"));
        var sql =
            "SELECT ba.book_id, a.display_name" +
            " FROM dbo.book_author AS ba" +
            " INNER JOIN dbo.author AS a ON a.id = ba.author_id" +
            $" WHERE ba.book_id IN ({idParameters})" +
            " ORDER BY ba.book_id, a.display_name";

        using (var command = new SqlCommand(sql, connection))
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
                    var displayName = reader.GetString(1);
                    authorsByBookId[bookId].Add(displayName);
                }
            }
        }

        foreach (var book in books)
        {
            book.Authors.AddRange(authorsByBookId[book.Id]);
        }
    }
}
