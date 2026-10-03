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

            var widgets = WidgetSeedData.CreateWidgets();
            var tags = widgets.SelectMany(w => w.Tags).OrderBy(t => t.Id).ToList();

            foreach (var w in widgets)
            {
                Insert(connection, bind, insertWidgetSql,
                    ("id", w.Id), ("name", w.Name), ("age", w.Age), ("amount", w.Amount), ("active", w.Active),
                    ("created", w.Created), ("externalId", w.ExternalId), ("opens", w.Opens),
                    ("notes", w.Notes ?? (object)DBNull.Value), ("code", w.Code));
            }

            foreach (var t in tags)
            {
                Insert(connection, bind, insertTagSql, ("id", t.Id), ("widgetId", t.WidgetId), ("label", t.Label), ("score", t.Score));
            }

            foreach (var m in tags.SelectMany(t => t.TagMeta).OrderBy(m => m.Id))
            {
                Insert(connection, bind, insertMetaSql, ("id", m.Id), ("tagId", m.TagId), ("kind", m.Kind), ("value", m.Value));
            }
        }

        private static void Insert(DbConnection connection, Action<DbCommand, string, object> bind, string sql, params (string Name, object Value)[] values)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, value) in values)
            {
                bind(cmd, name, value);
            }

            cmd.ExecuteNonQuery();
        }
    }
}
