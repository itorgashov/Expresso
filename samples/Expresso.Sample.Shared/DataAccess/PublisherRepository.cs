using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Sample.Shared.Models;
using Expresso.Rendering;

namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Loads publishers from the sample database.</summary>
public sealed class PublisherRepository : IRepository<Publisher>
{
    private const string WhereParamPrefix = "wparam";
    private const string OrderParamPrefix = "oparam";

    private readonly ISampleDb _db;
    private readonly IExpressionToQueryClauseTransformer _criteriaTransformer;
    private readonly Dictionary<string, string> _fieldToColumnMapping;
    private readonly string _baseSelect;

    /// <summary>Creates the repository.</summary>
    /// <param name="db">Open and bind helper for the selected engine.</param>
    /// <param name="criteriaTransformer">Renderer that turns filters and sorts into SQL.</param>
    public PublisherRepository(
        ISampleDb db,
        IExpressionToQueryClauseTransformer criteriaTransformer)
    {
        _db = db;
        _criteriaTransformer = criteriaTransformer;
        var sql = db.Sql;
        _fieldToColumnMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "name", sql.Col("p", "name") },
            { "country", sql.Col("p", "country") },
            { "location", sql.Col("p", "location") },
            { "opens", sql.Col("p", "opens_at") },
            { "closes", sql.Col("p", "closes_at") },
        };
        _baseSelect =
            "SELECT" +
            " " + sql.Col("p", "id") + "," +
            " " + sql.Col("p", "name") + "," +
            " " + sql.Col("p", "country") + "," +
            " " + sql.Col("p", "location") + "," +
            " " + sql.Col("p", "opens_at") + "," +
            " " + sql.Col("p", "closes_at") +
            " FROM " + sql.TableAs("publisher", "p");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Publisher>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        CancellationToken cancellationToken = default)
    {
        var connection = await _db.OpenAsync(cancellationToken);
        using (connection)
        {
            var (sql, parameters) = BuildSelectQuery(filterCriteria, sortDirective);

            var publishers = new List<Publisher>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                _db.BindAll(command, parameters);
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

    /// <inheritdoc />
    public async Task<Publisher?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var connection = await _db.OpenAsync(cancellationToken);
        using (connection)
        {
            var sql = _baseSelect + " WHERE " + _db.Sql.Col("p", "id") + " = " + _db.Sql.Param("id");

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                _db.Bind(command, _db.Sql.Param("id"), id);
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
        var sql = new StringBuilder(_baseSelect);
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
            ParameterMerge.Merge(parameters, result.parameters);
        }

        return (sql.ToString(), parameters);
    }

    private static Publisher ReadPublisher(DbDataReader reader) =>
        new Publisher
        {
            Id = SampleDbValues.GetInt32(reader, 0),
            Name = reader.GetString(1),
            Country = reader.GetString(2),
            Location = reader.IsDBNull(3) ? null : reader.GetString(3),
            OpensAt = SampleDbValues.GetTimeSpan(reader, 4),
            ClosesAt = SampleDbValues.GetTimeSpan(reader, 5),
        };
}
