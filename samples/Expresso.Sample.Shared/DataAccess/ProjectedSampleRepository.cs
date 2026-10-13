using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;
using Expresso.Rendering;

namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Executes minimal ADO.NET projection plans and attaches only requested collections.</summary>
public abstract class ProjectedSampleRepository<T> : IRepository<T>
{
    private readonly ISampleDb _db;
    private readonly IExpressionToQueryClauseTransformer _renderer;
    private readonly IProjectionToSqlTransformer _projection;
    private readonly SqlQueryMapping _query;
    private readonly SqlProjectionMapping _mapping;
    private readonly string _from;
    private readonly string _id;
    /// <summary>Creates a repository from trusted SQL and response mappings.</summary>
    protected ProjectedSampleRepository(ISampleDb db, IExpressionToQueryClauseTransformer renderer,
        IProjectionToSqlTransformer projection, SqlQueryMapping query, SqlProjectionMapping mapping, string from, string id)
    {
        _db = db;
        _renderer = renderer;
        _projection = projection;
        _query = query;
        _mapping = mapping;
        _from = from;
        _id = id;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllAsync(FilterCriteria? filterCriteria,
        SortDirective? sortDirective, PagingDirective paging, ProjectionDirective projection, CancellationToken cancellationToken = default)
    {
        var plan = _projection.RenderProjection(projection, _mapping);
        var text = new StringBuilder("SELECT " + plan.SelectList + " FROM " + _from);
        var parameters = new Dictionary<string, object>();
        AppendWhere(text, parameters, filterCriteria);
        var sort = SampleStableSort.ForPaging(sortDirective, paging);
        if (sort is not null && sort.Items.Count > 0)
        {
            var order = _renderer.RenderOrderByClause(sort, _query, "order");
            text.Append(" ORDER BY ").Append(order.orderByClause);
            ParameterMerge.Merge(parameters, order.parameters);
        }
        Dictionary<string, object>? pagingParameters = parameters;
        SamplePagingSql.Append(text, ref pagingParameters, _renderer, paging);
        using var connection = await _db.OpenAsync(cancellationToken);
        return await new SqlProjectionExecutor(_projection, _renderer).ExecuteAsync(plan, text.ToString(), pagingParameters!,
            new Reader(_db, connection), sortDirective, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, object?>?> GetByIdAsync(int id, ProjectionDirective projection,
        CancellationToken cancellationToken = default)
    {
        var plan = _projection.RenderProjection(projection, _mapping);
        using var connection = await _db.OpenAsync(cancellationToken);
        var rows = await new SqlProjectionExecutor(_projection, _renderer).ExecuteAsync(plan,
            "SELECT " + plan.SelectList + " FROM " + _from + " WHERE " + _id + " = " + _db.Sql.Param("id"),
            new Dictionary<string, object> { [_db.Sql.Param("id")] = id }, new Reader(_db, connection), cancellationToken: cancellationToken);
        return rows.FirstOrDefault();
    }

    /// <inheritdoc />
    public Task<long> CountAsync(FilterCriteria? filterCriteria, CancellationToken cancellationToken = default)
    {
        var text = new StringBuilder("SELECT COUNT(*) FROM " + _from);
        var parameters = new Dictionary<string, object>();
        AppendWhere(text, parameters, filterCriteria);
        return SampleCount.ReadAsync(_db, text.ToString(), parameters, cancellationToken);
    }
    private void AppendWhere(StringBuilder text, Dictionary<string, object> parameters, FilterCriteria? filter)
    {
        if (filter is null) return;
        var where = _renderer.RenderWhereClause(filter, _query, "where");
        text.Append(" WHERE ").Append(where.whereClause);
        ParameterMerge.Merge(parameters, where.parameters);
    }

    private sealed class Reader : IProjectionSqlQueryExecutor
    {
        private readonly ISampleDb _db;
        private readonly DbConnection _connection;
        internal Reader(ISampleDb db, DbConnection connection)
        {
            _db = db;
            _connection = connection;
        }
        public async Task<IReadOnlyList<object?[]>> ReadAsync(string sql, IReadOnlyDictionary<string, object> parameters,
            RenderedSqlProjection plan, CancellationToken cancellationToken)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            _db.BindAll(command, parameters.ToDictionary(p => p.Key, p => p.Value));
            var rows = new List<object?[]>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) rows.Add(plan.Columns.Select(c => Value(reader, c)).ToArray());
            return rows;
        }
    }

    private static object? Value(DbDataReader reader, SqlProjectedColumn column)
    {
        if (reader.IsDBNull(column.Ordinal)) return null;
        var type = Nullable.GetUnderlyingType(column.ValueType) ?? column.ValueType;
        if (type == typeof(DateTime)) return SampleDbValues.GetDateTime(reader, column.Ordinal);
        if (type == typeof(TimeSpan)) return SampleDbValues.GetTimeSpan(reader, column.Ordinal);
        if (type == typeof(Guid)) return SampleDbValues.GetGuid(reader, column.Ordinal);
        return Convert.ChangeType(reader.GetValue(column.Ordinal), type, CultureInfo.InvariantCulture);
    }

}
