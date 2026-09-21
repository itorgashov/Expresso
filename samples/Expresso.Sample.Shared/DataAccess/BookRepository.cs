using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Sample.Shared.Models;
using Expresso.Rendering;

namespace Expresso.Sample.Shared.DataAccess;

public sealed class BookRepository : IRepository<Book>
{
    private const string WhereParamPrefix = "wparam";
    private const string OrderParamPrefix = "oparam";

    private readonly ISampleDb _db;
    private readonly IExpressionToQueryClauseTransformer _criteriaTransformer;
    private readonly SqlQueryMapping _queryMapping;
    private readonly string _baseSelect;

    public BookRepository(
        ISampleDb db,
        IExpressionToQueryClauseTransformer criteriaTransformer)
    {
        _db = db;
        _criteriaTransformer = criteriaTransformer;
        var sql = db.Sql;
        var mappings = new SampleSqlMappings(sql);
        _queryMapping = new SqlQueryMapping(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "title", sql.Col("b", "title") },
                { "year", sql.Col("b", "year") },
                { "isbn", sql.Col("b", "isbn") },
                { "publisher", sql.Col("p", "name") },
                { "price", sql.Col("b", "price") },
                { "rating", sql.Col("b", "rating") },
                { "createdat", sql.Col("b", "created_at") },
                { "externalid", sql.Col("b", "external_id") },
            },
            new[] { mappings.BookAuthors });
        _baseSelect =
            "SELECT" +
            " " + sql.Col("b", "id") + "," +
            " " + sql.Col("b", "title") + "," +
            " " + sql.Col("b", "year") + "," +
            " " + sql.Col("b", "isbn") + "," +
            " " + sql.Col("b", "price") + "," +
            " " + sql.Col("b", "rating") + "," +
            " " + sql.Col("b", "created_at") + "," +
            " " + sql.Col("b", "external_id") + "," +
            " " + sql.Col("p", "name") + " AS publisher_name" +
            " FROM " + sql.TableAs("book", "b") +
            " INNER JOIN " + sql.TableAs("publisher", "p") + " ON " + sql.Col("p", "id") + " = " + sql.Col("b", "publisher_id");
    }

    public async Task<IReadOnlyList<Book>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        CancellationToken cancellationToken = default)
    {
        var connection = await _db.OpenAsync(cancellationToken);
        using (connection)
        {
            var (sql, parameters) = BuildSelectQuery(filterCriteria, sortDirective);

            var books = new List<Book>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                _db.BindAll(command, parameters);
                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        books.Add(ReadBook(reader));
                    }
                }
            }

            await BookChildLoader.LoadAuthorsAndAwardsAsync(
                connection,
                _db,
                books,
                sortDirective,
                _criteriaTransformer,
                cancellationToken);
            return books;
        }
    }

    public async Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var connection = await _db.OpenAsync(cancellationToken);
        using (connection)
        {
            var sql = _baseSelect + " WHERE " + _db.Sql.Col("b", "id") + " = " + _db.Sql.Param("id");

            Book? book = null;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                _db.Bind(command, _db.Sql.Param("id"), id);
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

            await BookChildLoader.LoadAuthorsAndAwardsAsync(
                connection,
                _db,
                new List<Book> { book },
                sortDirective: null,
                _criteriaTransformer,
                cancellationToken);
            return book;
        }
    }

    private (string sql, Dictionary<string, object>? parameters) BuildSelectQuery(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective)
    {
        var sql = new StringBuilder(_baseSelect);
        Dictionary<string, object>? parameters = null;

        if (filterCriteria is not null)
        {
            var result = _criteriaTransformer.RenderWhereClause(filterCriteria, _queryMapping, WhereParamPrefix);
            sql.Append(" WHERE ");
            sql.Append(result.whereClause);
            parameters = new Dictionary<string, object>(result.parameters);
        }

        if (sortDirective is not null && sortDirective.Items.Count > 0)
        {
            var result = _criteriaTransformer.RenderOrderByClause(sortDirective, _queryMapping, OrderParamPrefix);
            sql.Append(" ORDER BY ");
            sql.Append(result.orderByClause);
            parameters ??= new Dictionary<string, object>();
            ParameterMerge.Merge(parameters, result.parameters);
        }

        return (sql.ToString(), parameters);
    }

    private static Book ReadBook(DbDataReader reader) =>
        new Book
        {
            Id = SampleDbValues.GetInt32(reader, 0),
            Title = reader.GetString(1),
            Year = SampleDbValues.GetInt16(reader, 2),
            Isbn = reader.IsDBNull(3) ? null : reader.GetString(3),
            Price = SampleDbValues.GetDecimal(reader, 4),
            Rating = SampleDbValues.GetDouble(reader, 5),
            CreatedAt = SampleDbValues.GetDateTime(reader, 6),
            ExternalId = SampleDbValues.GetGuid(reader, 7),
            Publisher = reader.GetString(8),
        };
}
