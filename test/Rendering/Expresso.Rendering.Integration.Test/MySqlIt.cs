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
#if NET8_0_OR_GREATER
            EfSession = MySqlSeed.EfSession(_connection, "MySql");
#endif
#if NETFRAMEWORK
            Ef6Session = MySqlSeed.Ef6Session("MySql");
#endif
        }

        public IEngineSession? EfSession { get; }

        public IEngineSession? Ef6Session { get; }

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
#if NET8_0_OR_GREATER
            EfSession = MySqlSeed.EfSession(_connection, "MariaDb");
#endif
#if NETFRAMEWORK
            Ef6Session = MySqlSeed.Ef6Session("MariaDb");
#endif
        }

        public IEngineSession? EfSession { get; }

        public IEngineSession? Ef6Session { get; }

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
#if NET8_0_OR_GREATER
        public static IEngineSession EfSession(MySqlConnection connection, string connectionStringName)
        {
            // Pomelo requires AllowUserVariables on the connection it uses, so EF opens its own connection.
            var connectionString = IntegrationEnabled.ConnectionString(connectionStringName)!.TrimEnd(';') + ";AllowUserVariables=True";
            var version = Microsoft.EntityFrameworkCore.ServerVersion.AutoDetect(connection);
            return new Ef.EfEngineSession(connection, (b, _) => Microsoft.EntityFrameworkCore.MySqlDbContextOptionsBuilderExtensions.UseMySql(b, connectionString, version));
        }
#endif
#if NETFRAMEWORK
        /// <summary>EF6 uses Oracle's MySql.Data driver (the only EF6 MySQL provider) on its own connection.</summary>
        public static IEngineSession Ef6Session(string connectionStringName)
        {
            var connectionString = IntegrationEnabled.ConnectionString(connectionStringName)!.Replace("SslMode=None", "SslMode=Disabled");
            return new Ef6.Ef6EngineSession(() => new MySql.Data.MySqlClient.MySqlConnection(connectionString));
        }
#endif

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
