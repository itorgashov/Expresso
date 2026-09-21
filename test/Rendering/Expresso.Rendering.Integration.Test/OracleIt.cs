using System.Data;
using Expresso.Rendering;
using Oracle.ManagedDataAccess.Client;

namespace Expresso.Rendering.Integration.Test
{
    [CollectionDefinition("OracleIT")]
    public sealed class OracleItCollection : ICollectionFixture<OracleItFixture>
    {
    }

    public sealed class OracleItFixture : IDisposable
    {
        private readonly OracleConnection? _connection;

        public EngineSession? Session { get; }

        public OracleItFixture()
        {
            if (!IntegrationEnabled.IsOn)
            {
                return;
            }

            _connection = new OracleConnection(OracleItConnectionString.Get());
            _connection.Open();
            using (var nls = _connection.CreateCommand())
            {
                nls.CommandText = "ALTER SESSION SET NLS_TERRITORY = 'AMERICA'";
                nls.ExecuteNonQuery();
            }

            WidgetDdl.ResetAndSeed(
                _connection,
                new[]
                {
                    "BEGIN EXECUTE IMMEDIATE 'DROP TABLE \"widget_tag_meta\"'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;",
                    "BEGIN EXECUTE IMMEDIATE 'DROP TABLE \"widget_tag\"'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;",
                    "BEGIN EXECUTE IMMEDIATE 'DROP TABLE \"widget\"'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;",
                },
                new[]
                {
                    "CREATE TABLE \"widget\" (\"id\" NUMBER(10) PRIMARY KEY, \"name\" VARCHAR2(100) NOT NULL, \"age\" NUMBER(10) NOT NULL, \"amount\" BINARY_DOUBLE NOT NULL, \"active\" NUMBER(1) NOT NULL, \"created_at\" TIMESTAMP NOT NULL, \"external_id\" RAW(16) NOT NULL, \"opens\" INTERVAL DAY TO SECOND NOT NULL, \"notes\" VARCHAR2(200) NULL, \"code\" NUMBER(3) NOT NULL)",
                    "CREATE TABLE \"widget_tag\" (\"id\" NUMBER(10) PRIMARY KEY, \"widget_id\" NUMBER(10) NOT NULL, \"label\" VARCHAR2(50) NOT NULL, \"score\" NUMBER(10) NOT NULL)",
                    "CREATE TABLE \"widget_tag_meta\" (\"id\" NUMBER(10) PRIMARY KEY, \"tag_id\" NUMBER(10) NOT NULL, \"kind\" VARCHAR2(50) NOT NULL, \"value\" VARCHAR2(50) NOT NULL)",
                },
                ParameterBinder.Colon,
                "INSERT INTO \"widget\" (\"id\",\"name\",\"age\",\"amount\",\"active\",\"created_at\",\"external_id\",\"opens\",\"notes\",\"code\") VALUES (:id,:name,:age,:amount,:active,:created,:externalId,:opens,:notes,:code)",
                "INSERT INTO \"widget_tag\" (\"id\",\"widget_id\",\"label\",\"score\") VALUES (:id,:widgetId,:label,:score)",
                "INSERT INTO \"widget_tag_meta\" (\"id\",\"tag_id\",\"kind\",\"value\") VALUES (:id,:tagId,:kind,:value)");

            Session = new EngineSession(
                _connection,
                new ExpressionToOracleQueryClauseTransformer(),
                WidgetMapping.CreateQuoted(),
                WidgetMapping.TagsOnly(),
                ParameterBinder.Colon,
                "\"widget\"",
                "\"widget_tag\"",
                "\"id\"",
                "\"label\"",
                "\"widget_id\"");
        }

        public void Dispose()
        {
            if (_connection is null)
            {
                return;
            }

            try
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    _connection.Close();
                }
            }
            finally
            {
                _connection.Dispose();
                OracleConnection.ClearAllPools();
            }
        }
    }

    internal static class OracleItConnectionString
    {
        public static string Get()
        {
            var connectionString = IntegrationEnabled.ConnectionString("Oracle")
                ?? throw new InvalidOperationException("IntegrationTests:ConnectionStrings:Oracle is not configured.");

            if (connectionString.IndexOf("Pooling=", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return connectionString;
            }

            return connectionString.TrimEnd(';') + ";Pooling=false";
        }
    }

    [Trait("Category", "Integration")]
    [Collection("OracleIT")]
    public sealed class OracleItTests : EngineItTests
    {
        public OracleItTests(OracleItFixture fixture)
        {
            Session = fixture.Session!;
        }

        protected override IEngineSession Session { get; }
    }
}
