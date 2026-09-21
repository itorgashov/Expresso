using Expresso.Rendering;
using MySqlConnector;

namespace Expresso.Rendering.Integration.Test
{
    [CollectionDefinition("MySqlIT")]
    public sealed class MySqlItCollection : ICollectionFixture<MySqlItFixture>
    {
    }

    public sealed class MySqlItFixture : IDisposable
    {
        private readonly MySqlConnection? _connection;

        public EngineSession? Session { get; }

        public MySqlItFixture()
        {
            if (!IntegrationEnabled.IsOn)
            {
                return;
            }

            _connection = new MySqlConnection(IntegrationEnabled.ConnectionString("MySql"));
            _connection.Open();
            MySqlSeed.Apply(_connection);
            Session = new EngineSession(_connection, new ExpressionToMySqlQueryClauseTransformer(), WidgetMapping.Create(), WidgetMapping.TagsOnly(), ParameterBinder.At);
        }

        public void Dispose() => _connection?.Dispose();
    }

    [Trait("Category", "Integration")]
    [Collection("MySqlIT")]
    public sealed class MySqlItTests : EngineItTests
    {
        public MySqlItTests(MySqlItFixture fixture)
        {
            Session = fixture.Session!;
        }

        protected override IEngineSession Session { get; }
    }

    [CollectionDefinition("MariaDbIT")]
    public sealed class MariaDbItCollection : ICollectionFixture<MariaDbItFixture>
    {
    }

    public sealed class MariaDbItFixture : IDisposable
    {
        private readonly MySqlConnection? _connection;

        public EngineSession? Session { get; }

        public MariaDbItFixture()
        {
            if (!IntegrationEnabled.IsOn)
            {
                return;
            }

            _connection = new MySqlConnection(IntegrationEnabled.ConnectionString("MariaDb"));
            _connection.Open();
            MySqlSeed.Apply(_connection);
            Session = new EngineSession(_connection, new ExpressionToMySqlQueryClauseTransformer(), WidgetMapping.Create(), WidgetMapping.TagsOnly(), ParameterBinder.At);
        }

        public void Dispose() => _connection?.Dispose();
    }

    [Trait("Category", "Integration")]
    [Collection("MariaDbIT")]
    public sealed class MariaDbItTests : EngineItTests
    {
        public MariaDbItTests(MariaDbItFixture fixture)
        {
            Session = fixture.Session!;
        }

        protected override IEngineSession Session { get; }
    }

    internal static class MySqlSeed
    {
        public static void Apply(MySqlConnection connection)
        {
            WidgetDdl.ResetAndSeed(
                connection,
                new[] { "DROP TABLE IF EXISTS widget_tag_meta", "DROP TABLE IF EXISTS widget_tag", "DROP TABLE IF EXISTS widget" },
                new[]
                {
                    "CREATE TABLE widget (id INT PRIMARY KEY, name VARCHAR(100) NOT NULL, age INT NOT NULL, amount DOUBLE NOT NULL, active TINYINT(1) NOT NULL, created_at DATETIME NOT NULL, external_id CHAR(36) NOT NULL, opens TIME NOT NULL, notes VARCHAR(200) NULL, code TINYINT NOT NULL)",
                    "CREATE TABLE widget_tag (id INT PRIMARY KEY, widget_id INT NOT NULL, label VARCHAR(50) NOT NULL, score INT NOT NULL)",
                    "CREATE TABLE widget_tag_meta (id INT PRIMARY KEY, tag_id INT NOT NULL, kind VARCHAR(50) NOT NULL, value VARCHAR(50) NOT NULL)",
                },
                ParameterBinder.At,
                "INSERT INTO widget (id,name,age,amount,active,created_at,external_id,opens,notes,code) VALUES (@id,@name,@age,@amount,@active,@created,@externalId,@opens,@notes,@code)",
                "INSERT INTO widget_tag (id,widget_id,label,score) VALUES (@id,@widgetId,@label,@score)",
                "INSERT INTO widget_tag_meta (id,tag_id,kind,value) VALUES (@id,@tagId,@kind,@value)");
        }
    }
}
