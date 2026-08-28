using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Sample.Shared.Models;
using Expresso.SqlServer;
using Microsoft.Data.SqlClient;

namespace Expresso.Sample.Shared.DataAccess;

public sealed class PublisherRepository : IRepository<Publisher>
{
    private const string WhereParamPrefix = "wparam";
    private const string OrderParamPrefix = "oparam";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IExpressionToQueryClauseTransformer _criteriaTransformer;

    private readonly Dictionary<string, string> _fieldToColumnMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "name", "p.name" },
        { "country", "p.country" },
        { "location", "p.location" },
    };

    private const string BaseSelect =
        "SELECT" +
        " p.id," +
        " p.name," +
        " p.country," +
        " p.location" +
        " FROM dbo.publisher AS p";

    public PublisherRepository(
        ISqlConnectionFactory connectionFactory,
        IExpressionToQueryClauseTransformer criteriaTransformer)
    {
        _connectionFactory = connectionFactory;
        _criteriaTransformer = criteriaTransformer;
    }

    public async Task<IReadOnlyList<Publisher>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using (connection)
        {
            var (sql, parameters) = BuildSelectQuery(filterCriteria, sortDirective);

            var publishers = new List<Publisher>();
            using (var command = new SqlCommand(sql, connection))
            {
                command.AddParameters(parameters);
                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        publishers.Add(ReadPublisher(reader));
                    }
                }
            }

            return publishers;
        }
    }

    public async Task<Publisher?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using (connection)
        {
            var sql = BaseSelect + " WHERE p.id = @id";

            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", id);
                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        return ReadPublisher(reader);
                    }
                }
            }

            return null;
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
            var result = _criteriaTransformer.RenderWhereClause(filterCriteria, _fieldToColumnMapping, WhereParamPrefix);
            sql.Append(" WHERE ");
            sql.Append(result.whereClause);
            parameters = new Dictionary<string, object>(result.parameters);
        }

        if (sortDirective is not null)
        {
            var result = _criteriaTransformer.RenderOrderByClause(sortDirective, _fieldToColumnMapping, OrderParamPrefix);
            sql.Append(" ORDER BY ");
            sql.Append(result.orderByClause);
            parameters ??= new Dictionary<string, object>();
            SqlParameterExtensions.MergeParameters(parameters, result.parameters);
        }

        return (sql.ToString(), parameters);
    }

    private static Publisher ReadPublisher(SqlDataReader reader) =>
        new Publisher
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Country = reader.GetString(2),
            Location = reader.IsDBNull(3) ? null : reader.GetString(3),
        };
}
