using Expresso.Rendering;
using Microsoft.Data.SqlClient;

namespace Expresso.Rendering.Integration.Test
{
    [CollectionDefinition("SqlServerIT")]
    public sealed class SqlServerItCollection : ICollectionFixture<SqlServerItFixture>
    {
    }

    public sealed class SqlServerItFixture : IDisposable
    {
        private readonly SqlConnection? _connection;

        public EngineSession? Session { get; }

        public SqlServerItFixture()
        {
            if (!IntegrationEnabled.IsOn)
            {
                return;
            }

            _connection = new SqlConnection(IntegrationEnabled.ConnectionString("SqlServer"));
            _connection.Open();
            WidgetDdl.ResetAndSeed(
                _connection,
                new[]
                {
                    "IF OBJECT_ID('widget_tag_meta','U') IS NOT NULL DROP TABLE widget_tag_meta",
                    "IF OBJECT_ID('widget_tag','U') IS NOT NULL DROP TABLE widget_tag",
                    "IF OBJECT_ID('widget','U') IS NOT NULL DROP TABLE widget",
                },
                new[]
                {
                    """
                    CREATE TABLE widget (
                      id INT NOT NULL PRIMARY KEY,
                      name NVARCHAR(100) NOT NULL,
                      age INT NOT NULL,
                      amount FLOAT NOT NULL,
                      active BIT NOT NULL,
                      created_at DATETIME2 NOT NULL,
                      external_id UNIQUEIDENTIFIER NOT NULL,
                      opens TIME NOT NULL,
                      notes NVARCHAR(200) NULL,
                      code TINYINT NOT NULL)
                    """,
                    """
                    CREATE TABLE widget_tag (
                      id INT NOT NULL PRIMARY KEY,
                      widget_id INT NOT NULL,
                      label NVARCHAR(50) NOT NULL,
                      score INT NOT NULL)
                    """,
                    """
                    CREATE TABLE widget_tag_meta (
                      id INT NOT NULL PRIMARY KEY,
                      tag_id INT NOT NULL,
                      kind NVARCHAR(50) NOT NULL,
                      value NVARCHAR(50) NOT NULL)
                    """,
                },
                ParameterBinder.At,
                "INSERT INTO widget (id,name,age,amount,active,created_at,external_id,opens,notes,code) VALUES (@id,@name,@age,@amount,@active,@created,@externalId,@opens,@notes,@code)",
                "INSERT INTO widget_tag (id,widget_id,label,score) VALUES (@id,@widgetId,@label,@score)",
                "INSERT INTO widget_tag_meta (id,tag_id,kind,value) VALUES (@id,@tagId,@kind,@value)");
            Session = new EngineSession(_connection, new ExpressionToSqlServerQueryClauseTransformer(), WidgetMapping.Create(), WidgetMapping.TagsOnly(), ParameterBinder.At);
        }

        public void Dispose() => _connection?.Dispose();
    }

    [Trait("Category", "Integration")]
    [Collection("SqlServerIT")]
    public sealed class SqlServerItTests : EngineItTests
    {
        public SqlServerItTests(SqlServerItFixture fixture)
        {
            Session = fixture.Session!;
        }

        protected override IEngineSession Session { get; }
    }
}
