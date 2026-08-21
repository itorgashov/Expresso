using System.Text;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Sample.WebApi.Models;
using Expresso.SqlServer;
using Microsoft.Data.SqlClient;

namespace Expresso.Sample.WebApi.DataAccess;

public sealed class AuthorRepository(
    ISqlConnectionFactory connectionFactory,
    IExpressionToQueryClauseTransformer criteriaTransformer) : IRepository<Author>
{
    private const string WhereParamPrefix = "wparam";
    private const string OrderParamPrefix = "oparam";

    private readonly Dictionary<string, string> _fieldToColumnMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "firstname", "a.first_name" },
        { "lastname", "a.last_name" },
        { "displayname", "a.display_name" },
        { "dateofbirth", "a.date_of_birth" },
    };

    private const string BaseSelect = """
        SELECT
            a.id,
            a.first_name,
            a.last_name,
            a.display_name,
            a.date_of_birth,
            a.date_of_death,
            a.created_at
        FROM dbo.author AS a
        """;

    public async Task<IReadOnlyList<Author>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var (sql, parameters) = BuildSelectQuery(filterCriteria, sortDirective);

        var authors = new List<Author>();
        await using var command = new SqlCommand(sql, connection);
        command.AddParameters(parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            authors.Add(ReadAuthor(reader));
        }

        return authors;
    }

    public async Task<Author?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = BaseSelect + " WHERE a.id = @id";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return ReadAuthor(reader);
        }

        return null;
    }

    private (string sql, Dictionary<string, object>? parameters) BuildSelectQuery(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective)
    {
        var sql = new StringBuilder(BaseSelect);
        Dictionary<string, object>? parameters = null;

        if (filterCriteria is not null)
        {
            var result = criteriaTransformer.RenderWhereClause(filterCriteria, _fieldToColumnMapping, WhereParamPrefix);
            sql.Append(" WHERE ");
            sql.Append(result.whereClause);
            parameters = new Dictionary<string, object>(result.parameters);
        }

        if (sortDirective is not null)
        {
            var result = criteriaTransformer.RenderOrderByClause(sortDirective, _fieldToColumnMapping, OrderParamPrefix);
            sql.Append(" ORDER BY ");
            sql.Append(result.orderByClause);
            parameters ??= new Dictionary<string, object>();
            foreach (var pair in result.parameters)
            {
                parameters.TryAdd(pair.Key, pair.Value);
            }
        }

        return (sql.ToString(), parameters);
    }

    private static Author ReadAuthor(SqlDataReader reader) =>
        new()
        {
            Id = reader.GetInt32(0),
            FirstName = reader.GetString(1),
            LastName = reader.GetString(2),
            DisplayName = reader.GetString(3),
            DateOfBirth = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            DateOfDeath = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
            CreatedAt = reader.GetDateTime(6),
        };
}
