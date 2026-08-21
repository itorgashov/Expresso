using System.Text;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Sample.WebApi.Models;
using Expresso.SqlServer;
using Microsoft.Data.SqlClient;

namespace Expresso.Sample.WebApi.DataAccess;

public sealed class PublisherRepository(
    ISqlConnectionFactory connectionFactory,
    IExpressionToQueryClauseTransformer criteriaTransformer) : IRepository<Publisher>
{
    private const string WhereParamPrefix = "wparam";
    private const string OrderParamPrefix = "oparam";

    private readonly Dictionary<string, string> _fieldToColumnMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "name", "p.name" },
        { "country", "p.country" },
        { "location", "p.location" },
    };

    private const string BaseSelect = """
        SELECT
            p.id,
            p.name,
            p.country,
            p.location
        FROM dbo.publisher AS p
        """;

    public async Task<IReadOnlyList<Publisher>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var (sql, parameters) = BuildSelectQuery(filterCriteria, sortDirective);

        var publishers = new List<Publisher>();
        await using var command = new SqlCommand(sql, connection);
        command.AddParameters(parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            publishers.Add(ReadPublisher(reader));
        }

        return publishers;
    }

    public async Task<Publisher?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = BaseSelect + " WHERE p.id = @id";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return ReadPublisher(reader);
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

    private static Publisher ReadPublisher(SqlDataReader reader) =>
        new()
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Country = reader.GetString(2),
            Location = reader.IsDBNull(3) ? null : reader.GetString(3),
        };
}
