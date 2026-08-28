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

public sealed class AuthorRepository : IRepository<Author>
{
    private const string WhereParamPrefix = "wparam";
    private const string OrderParamPrefix = "oparam";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IExpressionToQueryClauseTransformer _criteriaTransformer;

    private readonly Dictionary<string, string> _fieldToColumnMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "firstname", "a.first_name" },
        { "lastname", "a.last_name" },
        { "displayname", "a.display_name" },
        { "dateofbirth", "a.date_of_birth" },
        { "createdat", "a.created_at" },
    };

    private const string BaseSelect =
        "SELECT" +
        " a.id," +
        " a.first_name," +
        " a.last_name," +
        " a.display_name," +
        " a.date_of_birth," +
        " a.created_at" +
        " FROM dbo.author AS a";

    public AuthorRepository(
        ISqlConnectionFactory connectionFactory,
        IExpressionToQueryClauseTransformer criteriaTransformer)
    {
        _connectionFactory = connectionFactory;
        _criteriaTransformer = criteriaTransformer;
    }

    public async Task<IReadOnlyList<Author>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using (connection)
        {
            var (sql, parameters) = BuildSelectQuery(filterCriteria, sortDirective);

            var authors = new List<Author>();
            using (var command = new SqlCommand(sql, connection))
            {
                command.AddParameters(parameters);
                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        authors.Add(ReadAuthor(reader));
                    }
                }
            }

            return authors;
        }
    }

    public async Task<Author?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using (connection)
        {
            var sql = BaseSelect + " WHERE a.id = @id";

            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", id);
                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        return ReadAuthor(reader);
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

    private static Author ReadAuthor(SqlDataReader reader) =>
        new Author
        {
            Id = reader.GetInt32(0),
            FirstName = reader.GetString(1),
            LastName = reader.GetString(2),
            DisplayName = reader.GetString(3),
            DateOfBirth = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            CreatedAt = reader.GetDateTime(5),
        };
}
