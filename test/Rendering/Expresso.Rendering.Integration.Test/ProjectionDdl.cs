using System.Data.Common;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.Integration.Test;

/// <summary>Creates and seeds only integration-owned widget_projection_* objects.</summary>
internal static class ProjectionDdl
{
    internal static void Reset(DbConnection connection, Action<DbCommand, string, object> bind)
    {
        var name = connection.GetType().Name;
        var oracle = name.IndexOf("Oracle", StringComparison.OrdinalIgnoreCase) >= 0;
        var mysql = name.IndexOf("MySql", StringComparison.OrdinalIgnoreCase) >= 0;
        var sqlServer = name == "SqlConnection";
        var payloadType = sqlServer ? "XML" : oracle || name.IndexOf("Db2", StringComparison.OrdinalIgnoreCase) >= 0
            ? "CLOB" : name.IndexOf("Npgsql", StringComparison.OrdinalIgnoreCase) >= 0 ? "JSON" : "TEXT";
        string Q(string value) => mysql ? "`" + value + "`" : sqlServer ? "[" + value + "]" : "\"" + value + "\"";
        string Table(string suffix) => Q("widget_projection_" + suffix);
        foreach (var table in new[] { "leaf", "child", "parent" })
        {
            try { Execute("DROP TABLE " + Table(table)); }
            catch (DbException e) when (Missing(e.Message)) { }
        }
        Execute("CREATE TABLE " + Table("parent") + " (" + Q("tenant") + " INT NOT NULL, " + Q("id") +
            " INT NOT NULL, " + Q("title") + " VARCHAR(100) NOT NULL, " + Q("notes") + " VARCHAR(100) NULL, PRIMARY KEY (" + Q("tenant") + "," + Q("id") + "))");
        Execute("CREATE TABLE " + Table("child") + " (" + Q("tenant") + " INT NOT NULL, " + Q("id") +
            " INT NOT NULL, " + Q("parent_id") + " INT NOT NULL, " + Q("name") + " VARCHAR(100) NOT NULL, " +
            Q("payload") + " " + payloadType + " NULL, PRIMARY KEY (" +
            Q("tenant") + "," + Q("id") + "), FOREIGN KEY (" + Q("tenant") + "," + Q("parent_id") + ") REFERENCES " +
            Table("parent") + "(" + Q("tenant") + "," + Q("id") + "))");
        Execute("CREATE TABLE " + Table("leaf") + " (" + Q("tenant") + " INT NOT NULL, " + Q("id") +
            " INT NOT NULL, " + Q("child_id") + " INT NOT NULL, " + Q("value") + " VARCHAR(100) NOT NULL, PRIMARY KEY (" +
            Q("tenant") + "," + Q("id") + "), FOREIGN KEY (" + Q("tenant") + "," + Q("child_id") + ") REFERENCES " +
            Table("child") + "(" + Q("tenant") + "," + Q("id") + "))");
        foreach (var parent in ProjectionFixture.Data())
        {
            Insert("parent", ("tenant", parent.Tenant), ("id", parent.Id), ("title", parent.Title), ("notes", parent.Notes ?? (object)DBNull.Value));
            foreach (var child in parent.Children)
            {
                Insert("child", ("tenant", child.Tenant), ("id", child.Id), ("parent_id", child.ParentId), ("name", child.Name));
                foreach (var leaf in child.Leaves)
                    Insert("leaf", ("tenant", leaf.Tenant), ("id", leaf.Id), ("child_id", leaf.ChildId), ("value", leaf.Value));
            }
        }
        void Execute(string sql) { using var command = connection.CreateCommand(); command.CommandText = sql; command.ExecuteNonQuery(); }
        void Insert(string table, params (string Name, object Value)[] values)
        {
            using var command = connection.CreateCommand();
            var prefix = oracle ? ":" : "@";
            command.CommandText = "INSERT INTO " + Table(table) + " (" + string.Join(",", values.Select(v => Q(v.Name))) +
                ") VALUES (" + string.Join(",", values.Select(v => prefix + v.Name)) + ")";
            foreach (var value in values) bind(command, prefix + value.Name, value.Value);
            command.ExecuteNonQuery();
        }
    }
    private static bool Missing(string message) => new[] { "does not exist", "Unknown table", "Cannot drop the table", "no such table", "ORA-00942", "SQL0204N" }
        .Any(text => message.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);
}
