using Expresso.Rendering;
using Microsoft.Data.Sqlite;

namespace Expresso.Rendering.Integration.Test
{
    [CollectionDefinition("SqliteIT")]
    public sealed class SqliteItCollection : ICollectionFixture<SqliteItFixture>
    {
    }

    public sealed class SqliteItFixture : IDisposable
    {
        private readonly string _path;
        private readonly SqliteConnection? _connection;

        public EngineSession? Session { get; }

        public SqliteItFixture()
        {
            _path = Path.Combine(Path.GetTempPath(), "expresso-it-" + Guid.NewGuid().ToString("N") + ".db");
            if (!IntegrationEnabled.IsOn)
            {
                return;
            }

            _connection = new SqliteConnection("Data Source=" + _path + ";Pooling=False");
            _connection.Open();
            WidgetDdl.ResetAndSeed(
                _connection,
                new[] { "DROP TABLE IF EXISTS widget_tag_meta", "DROP TABLE IF EXISTS widget_tag", "DROP TABLE IF EXISTS widget" },
                new[]
                {
                    "CREATE TABLE widget (id INTEGER PRIMARY KEY, name TEXT NOT NULL, age INTEGER NOT NULL, amount REAL NOT NULL, active INTEGER NOT NULL, created_at TEXT NOT NULL, external_id TEXT NOT NULL, opens TEXT NOT NULL, notes TEXT NULL, code INTEGER NOT NULL)",
                    "CREATE TABLE widget_tag (id INTEGER PRIMARY KEY, widget_id INTEGER NOT NULL, label TEXT NOT NULL, score INTEGER NOT NULL)",
                    "CREATE TABLE widget_tag_meta (id INTEGER PRIMARY KEY, tag_id INTEGER NOT NULL, kind TEXT NOT NULL, value TEXT NOT NULL)",
                },
                ParameterBinder.At,
                "INSERT INTO widget (id,name,age,amount,active,created_at,external_id,opens,notes,code) VALUES (@id,@name,@age,@amount,@active,@created,@externalId,@opens,@notes,@code)",
                "INSERT INTO widget_tag (id,widget_id,label,score) VALUES (@id,@widgetId,@label,@score)",
                "INSERT INTO widget_tag_meta (id,tag_id,kind,value) VALUES (@id,@tagId,@kind,@value)");
            Session = new EngineSession(_connection, new ExpressionToSqliteQueryClauseTransformer(), WidgetMapping.Create(), WidgetMapping.TagsOnly(), ParameterBinder.At);
        }

        public void Dispose()
        {
            _connection?.Dispose();
            SqliteConnection.ClearAllPools();
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
    }

    [Trait("Category", "Integration")]
    [Collection("SqliteIT")]
    public sealed class SqliteItTests : EngineItTests
    {
        public SqliteItTests(SqliteItFixture fixture)
        {
            Session = fixture.Session!;
        }

        protected override IEngineSession Session { get; }
    }
}
