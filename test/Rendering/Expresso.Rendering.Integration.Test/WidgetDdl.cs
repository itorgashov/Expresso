using System.Data.Common;

namespace Expresso.Rendering.Integration.Test
{
    public static class WidgetDdl
    {
        public static void ResetAndSeed(
            DbConnection connection,
            string[] dropSql,
            string[] createSql,
            Action<DbCommand, string, object> bind,
            string insertWidgetSql,
            string insertTagSql,
            string insertMetaSql)
        {
            using (var drop = connection.CreateCommand())
            {
                foreach (var sql in dropSql)
                {
                    drop.CommandText = sql;
                    drop.ExecuteNonQuery();
                }
            }

            using (var create = connection.CreateCommand())
            {
                foreach (var sql in createSql)
                {
                    create.CommandText = sql;
                    create.ExecuteNonQuery();
                }
            }

            InsertWidget(connection, bind, insertWidgetSql, 1, "Alice", 30, 50.5, true, WidgetSeedData.Created1, WidgetSeedData.Guid1, WidgetSeedData.OpensMorning, null, (byte)1);
            InsertWidget(connection, bind, insertWidgetSql, 2, "Bob", 25, 40.0, false, WidgetSeedData.Created2, WidgetSeedData.Guid2, WidgetSeedData.OpensEvening, "100Xoff", 2);
            InsertWidget(connection, bind, insertWidgetSql, 3, "Carol", 30, 61.4, true, WidgetSeedData.Created3, WidgetSeedData.Guid3, WidgetSeedData.OpensMorning, "  pad  ", 3);
            InsertWidget(connection, bind, insertWidgetSql, 4, "Dave", 40, 10.0, true, WidgetSeedData.Created4, WidgetSeedData.Guid4, WidgetSeedData.OpensMidnight, "100%_off", 4);
            InsertWidget(connection, bind, insertWidgetSql, 5, "Eve", 0, -12.7, false, WidgetSeedData.Created5, WidgetSeedData.Guid5, WidgetSeedData.OpensNoon, "n/a", 5);
            InsertWidget(connection, bind, insertWidgetSql, 6, "Frank", 18, 99.9, true, WidgetSeedData.Created6, WidgetSeedData.Guid6, WidgetSeedData.OpensNineThirty, "a\\b", 6);

            InsertTag(connection, bind, insertTagSql, 1, 1, "red", 10);
            InsertTag(connection, bind, insertTagSql, 2, 1, "blue", 20);
            InsertTag(connection, bind, insertTagSql, 3, 2, "red", 5);
            InsertTag(connection, bind, insertTagSql, 4, 4, "green", 15);
            InsertTag(connection, bind, insertTagSql, 5, 4, "red", 15);
            InsertTag(connection, bind, insertTagSql, 6, 5, "yellow", 0);
            InsertTag(connection, bind, insertTagSql, 7, 6, "red", 100);

            InsertMeta(connection, bind, insertMetaSql, 1, 2, "size", "large");
            InsertMeta(connection, bind, insertMetaSql, 2, 1, "color", "primary");
        }

        private static void InsertWidget(
            DbConnection connection,
            Action<DbCommand, string, object> bind,
            string sql,
            int id,
            string name,
            int age,
            double amount,
            bool active,
            DateTime created,
            Guid externalId,
            TimeSpan opens,
            string? notes,
            byte code)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            bind(cmd, "id", id);
            bind(cmd, "name", name);
            bind(cmd, "age", age);
            bind(cmd, "amount", amount);
            bind(cmd, "active", active);
            bind(cmd, "created", created);
            bind(cmd, "externalId", externalId);
            bind(cmd, "opens", opens);
            bind(cmd, "notes", notes ?? (object)DBNull.Value);
            bind(cmd, "code", code);
            cmd.ExecuteNonQuery();
        }

        private static void InsertTag(DbConnection connection, Action<DbCommand, string, object> bind, string sql, int id, int widgetId, string label, int score)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            bind(cmd, "id", id);
            bind(cmd, "widgetId", widgetId);
            bind(cmd, "label", label);
            bind(cmd, "score", score);
            cmd.ExecuteNonQuery();
        }

        private static void InsertMeta(DbConnection connection, Action<DbCommand, string, object> bind, string sql, int id, int tagId, string kind, string value)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            bind(cmd, "id", id);
            bind(cmd, "tagId", tagId);
            bind(cmd, "kind", kind);
            bind(cmd, "value", value);
            cmd.ExecuteNonQuery();
        }
    }
}
