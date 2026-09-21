using Expresso.Rendering;
using Npgsql;

namespace Expresso.Rendering.Integration.Test
{
    [CollectionDefinition("PostgreSqlIT")]
    public sealed class PostgreSqlItCollection : ICollectionFixture<PostgreSqlItFixture>
    {
    }

    public sealed class PostgreSqlItFixture : IDisposable
    {
        private readonly NpgsqlConnection? _connection;

        public EngineSession? Session { get; }

        public PostgreSqlItFixture()
        {
            if (!IntegrationEnabled.IsOn)
            {
                return;
            }

            _connection = new NpgsqlConnection(IntegrationEnabled.ConnectionString("PostgreSql"));
            _connection.Open();
            WidgetDdl.ResetAndSeed(
                _connection,
                new[] { "DROP TABLE IF EXISTS widget_tag_meta", "DROP TABLE IF EXISTS widget_tag", "DROP TABLE IF EXISTS widget" },
                new[]
                {
                    "CREATE TABLE widget (id INT PRIMARY KEY, name VARCHAR(100) NOT NULL, age INT NOT NULL, amount DOUBLE PRECISION NOT NULL, active BOOLEAN NOT NULL, created_at TIMESTAMP NOT NULL, external_id UUID NOT NULL, opens TIME NOT NULL, notes VARCHAR(200) NULL, code SMALLINT NOT NULL)",
                    "CREATE TABLE widget_tag (id INT PRIMARY KEY, widget_id INT NOT NULL, label VARCHAR(50) NOT NULL, score INT NOT NULL)",
                    "CREATE TABLE widget_tag_meta (id INT PRIMARY KEY, tag_id INT NOT NULL, kind VARCHAR(50) NOT NULL, value VARCHAR(50) NOT NULL)",
                },
                ParameterBinder.At,
                "INSERT INTO widget (id,name,age,amount,active,created_at,external_id,opens,notes,code) VALUES (@id,@name,@age,@amount,@active,@created,@externalId,@opens,@notes,@code)",
                "INSERT INTO widget_tag (id,widget_id,label,score) VALUES (@id,@widgetId,@label,@score)",
                "INSERT INTO widget_tag_meta (id,tag_id,kind,value) VALUES (@id,@tagId,@kind,@value)");
            Session = new EngineSession(_connection, new ExpressionToPostgreSqlQueryClauseTransformer(), WidgetMapping.Create(), WidgetMapping.TagsOnly(), ParameterBinder.At);
        }

        public void Dispose() => _connection?.Dispose();
    }

    [Trait("Category", "Integration")]
    [Collection("PostgreSqlIT")]
    public sealed class PostgreSqlItTests : EngineItTests
    {
        public PostgreSqlItTests(PostgreSqlItFixture fixture)
        {
            Session = fixture.Session!;
        }

        protected override IEngineSession Session { get; }
    }
}
