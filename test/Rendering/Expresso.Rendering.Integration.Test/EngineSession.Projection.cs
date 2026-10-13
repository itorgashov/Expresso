using System.Data.Common;
using System.Globalization;
using Expresso.Core.Projection;

namespace Expresso.Rendering.Integration.Test;

public interface IProjectionEngineSession
{
    IReadOnlyList<string> ProjectionCommands { get; }
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryProjectionAsync(ProjectionDirective projection, bool windowed = false);
}

public sealed partial class EngineSession : IProjectionEngineSession
{
    private readonly List<string> _projectionCommands = new();
    public IReadOnlyList<string> ProjectionCommands => _projectionCommands;

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryProjectionAsync(ProjectionDirective projection, bool windowed = false)
    {
        _projectionCommands.Clear();
        // Use fixture-owned table/identifier quoting; no reference to sample applications.
        string Q(string name) => _connection.GetType().Name == "SqlConnection" ? "[" + name + "]" :
            _connection.GetType().Name == "MySqlConnection" ? "`" + name + "`" : "\"" + name + "\"";
        string C(string alias, string column) => Q(alias) + "." + Q(column);
        string From(string table, string alias) => Q(table) + " " + Q(alias);
        SqlProjectionMapping Identity(string alias) => new SqlProjectionMapping()
            .Key("tenant", C(alias, "tenant"), typeof(int)).Key("id", C(alias, "id"), typeof(int));
        SqlProjectionKey[] Correlation(string alias, string foreign) => new[] {
            new SqlProjectionKey("tenant", C(alias, "tenant"), typeof(int)),
            new SqlProjectionKey("id", C(alias, foreign), typeof(int)) };
        var leaf = Identity("l").Field("value", "value", C("l", "value"), typeof(string));
        var child = Identity("c").Field("name", "name", C("c", "name"), typeof(string))
            .Collection("leaves", "leaves", new SqlCollectionProjectionMapping(From("widget_projection_leaf", "l"), leaf,
                Correlation("l", "child_id"), C("l", "id")));
        var parent = Identity("p").Field("id", "id", C("p", "id"), typeof(int))
            .Field("title", "title", C("p", "title"), typeof(string)).Field("notes", "notes", C("p", "notes"), typeof(string))
            .Collection("children", "children", new SqlCollectionProjectionMapping(From("widget_projection_child", "c"), child,
                Correlation("c", "parent_id"), C("c", "name") + ", " + C("c", "id")));
        var projectionRenderer = (IProjectionToSqlTransformer)_transformer;
        var plan = projectionRenderer.RenderProjection(projection, parent);
        var text = "SELECT " + plan.SelectList + " FROM " + From("widget_projection_parent", "p");
        var parameters = new Dictionary<string, object>();
        if (windowed)
        {
            var rootMap = new SqlQueryMapping(new Dictionary<string, string> { ["title"] = "p.title" });
            var where = _transformer.RenderWhereClause(new Expresso.Core.Filtering.FilterCriteria {
                Expression = new Expresso.Core.CriteriaExpressions.NeqFunc(new Expresso.Core.CriteriaExpressions.Field("title", typeof(string)),
                    new Expresso.Core.CriteriaExpressions.Literal("empty")) }, rootMap, "root");
            text += " WHERE " + where.whereClause;
            foreach (var parameter in where.parameters) parameters.Add(parameter.Key, parameter.Value);
        }
        text += " ORDER BY " + C("p", "title") + " DESC, " + C("p", "tenant") + ", " + C("p", "id");
        if (windowed)
        {
            var page = _transformer.RenderPagingClause(new Expresso.Core.Paging.PagingDirective(skip: 1, take: 1), "pg");
            text += " " + page.pagingClause;
            foreach (var parameter in page.parameters) parameters.Add(parameter.Key, parameter.Value);
        }
        return await new SqlProjectionExecutor(projectionRenderer, _transformer).ExecuteAsync(plan, text, parameters,
            new ProjectionReader(_connection, _bind, _projectionCommands));
    }

    private sealed class ProjectionReader : IProjectionSqlQueryExecutor
    {
        private readonly DbConnection _connection;
        private readonly Action<DbCommand, string, object> _bind;
        private readonly List<string> _commands;
        internal ProjectionReader(DbConnection connection, Action<DbCommand, string, object> bind, List<string> commands)
        {
            _connection = connection;
            _bind = bind;
            _commands = commands;
        }
        public async Task<IReadOnlyList<object?[]>> ReadAsync(string sql, IReadOnlyDictionary<string, object> parameters,
            RenderedSqlProjection plan, CancellationToken cancellationToken)
        {
            _commands.Add(sql);
            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            foreach (var parameter in parameters) _bind(command, parameter.Key, parameter.Value);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var result = new List<object?[]>();
            while (await reader.ReadAsync(cancellationToken))
                result.Add(plan.Columns.Select(c => reader.IsDBNull(c.Ordinal) ? null :
                    Convert.ChangeType(reader.GetValue(c.Ordinal), Nullable.GetUnderlyingType(c.ValueType) ?? c.ValueType, CultureInfo.InvariantCulture)).ToArray());
            return result;
        }
    }
}
