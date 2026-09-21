#if NET6_0_OR_GREATER
using System.Data.Common;
using IBM.Data.Db2;
using Expresso.Rendering;

namespace Expresso.Rendering.Integration.Test
{
    [CollectionDefinition("Db2IT")]
    public sealed class Db2ItCollection : ICollectionFixture<Db2ItFixture>
    {
    }

    public sealed class Db2ItFixture : IDisposable
    {
        private readonly DB2Connection? _connection;

        public EngineSession? Session { get; }

        public Db2ItFixture()
        {
            if (!IntegrationEnabled.IsOn)
            {
                return;
            }

            _connection = new DB2Connection(IntegrationEnabled.ConnectionString("Db2"));
            _connection.Open();
            DropTableIfExists(_connection, "widget_tag_meta");
            DropTableIfExists(_connection, "widget_tag");
            DropTableIfExists(_connection, "widget");
            DropTableIfExists(_connection, "\"widget_tag_meta\"");
            DropTableIfExists(_connection, "\"widget_tag\"");
            DropTableIfExists(_connection, "\"widget\"");
            WidgetDdl.ResetAndSeed(
                _connection,
                Array.Empty<string>(),
                new[]
                {
                    "CREATE TABLE \"widget\" (\"id\" INT NOT NULL PRIMARY KEY, \"name\" VARCHAR(100) NOT NULL, \"age\" INT NOT NULL, \"amount\" DOUBLE NOT NULL, \"active\" SMALLINT NOT NULL, \"created_at\" TIMESTAMP NOT NULL, \"external_id\" CHAR(36) NOT NULL, \"opens\" TIME NOT NULL, \"notes\" VARCHAR(200), \"code\" SMALLINT NOT NULL)",
                    "CREATE TABLE \"widget_tag\" (\"id\" INT NOT NULL PRIMARY KEY, \"widget_id\" INT NOT NULL, \"label\" VARCHAR(50) NOT NULL, \"score\" INT NOT NULL)",
                    "CREATE TABLE \"widget_tag_meta\" (\"id\" INT NOT NULL PRIMARY KEY, \"tag_id\" INT NOT NULL, \"kind\" VARCHAR(50) NOT NULL, \"value\" VARCHAR(50) NOT NULL)",
                },
                ParameterBinder.At,
                "INSERT INTO \"widget\" (\"id\",\"name\",\"age\",\"amount\",\"active\",\"created_at\",\"external_id\",\"opens\",\"notes\",\"code\") VALUES (@id,@name,@age,@amount,@active,@created,@externalId,@opens,@notes,@code)",
                "INSERT INTO \"widget_tag\" (\"id\",\"widget_id\",\"label\",\"score\") VALUES (@id,@widgetId,@label,@score)",
                "INSERT INTO \"widget_tag_meta\" (\"id\",\"tag_id\",\"kind\",\"value\") VALUES (@id,@tagId,@kind,@value)");
            Session = new EngineSession(
                _connection,
                new ExpressionToDb2QueryClauseTransformer(),
                WidgetMapping.CreateQuoted("w"),
                WidgetMapping.TagsOnly(),
                ParameterBinder.At,
                "\"widget\" AS w",
                "\"widget_tag\"",
                "w.\"id\"",
                "\"label\"",
                "\"widget_id\"",
                orderByInSelectList: true);
        }

        public void Dispose() => _connection?.Dispose();

        private static void DropTableIfExists(DbConnection connection, string table)
        {
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "DROP TABLE " + table;
                cmd.ExecuteNonQuery();
            }
            catch (DB2Exception ex) when (ex.Message.Contains("SQL0204N", StringComparison.Ordinal))
            {
            }
        }
    }

    [Trait("Category", "Integration")]
    [Collection("Db2IT")]
    public sealed class Db2ItTests : EngineItTests
    {
        public Db2ItTests(Db2ItFixture fixture)
        {
            Session = fixture.Session!;
        }

        protected override IEngineSession Session { get; }
    }
}
#endif
