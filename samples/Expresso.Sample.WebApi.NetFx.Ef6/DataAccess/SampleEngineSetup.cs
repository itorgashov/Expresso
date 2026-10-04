using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using SQLiteConnection = System.Data.SQLite.SQLiteConnection;
using Expresso.Rendering.EntityFramework;
using Expresso.Sample.WebApi.NetFx.Ef6.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;

namespace Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

public static class SampleEngineSetup
{
    public static void AddSampleEngine(IServiceCollection services, IConfiguration configuration)
    {
        var configuredEngine = configuration["ExpressoSample:Engine"];
        var engine = SampleEngineParser.Parse(configuredEngine);
        if (engine == SampleEngine.Db2)
        {
            throw new InvalidOperationException(
                "Db2 is not supported on the EF6 sample host. Use Expresso.Sample.WebApi.EfCore (net10).");
        }

        var mariaDb = SampleEngineParser.IsMariaDb(configuredEngine);
        var connectionString = ConnectionStringAdjustments.ForEngine(
            engine,
            RequireConnectionString(configuration, configuredEngine),
            mariaDb);
        var invariant = ProviderInvariantName(engine);
        var schema = engine == SampleEngine.SqlServer ? "dbo" : null;

        services.AddEf6ExpressionTransformations(invariant);
        services.AddScoped<SampleEf6Context>(_ => CreateContext(engine, connectionString, schema));

        services.AddTransient<IRepository<Book>, BookRepository>();
        services.AddTransient<IRepository<Author>, AuthorRepository>();
        services.AddTransient<IRepository<Publisher>, PublisherRepository>();
    }

    private static SampleEf6Context CreateContext(SampleEngine engine, string connectionString, string? schema)
    {
        DbConnection connection = engine switch
        {
            SampleEngine.SqlServer => new SqlConnection(connectionString),
            SampleEngine.PostgreSql => new NpgsqlConnection(connectionString),
            SampleEngine.MySql => new MySqlConnection(connectionString),
            SampleEngine.Sqlite => new SQLiteConnection(connectionString),
            SampleEngine.Oracle => new OracleConnection(connectionString),
            _ => throw new InvalidOperationException("Unsupported sample engine: " + engine),
        };

        return new SampleEf6Context(connection, schema);
    }

    private static string ProviderInvariantName(SampleEngine engine) => engine switch
    {
        SampleEngine.SqlServer => "System.Data.SqlClient",
        SampleEngine.PostgreSql => "Npgsql",
        SampleEngine.MySql => "MySql.Data.MySqlClient",
        SampleEngine.Sqlite => "System.Data.SQLite.EF6",
        SampleEngine.Oracle => "Oracle.ManagedDataAccess.Client",
        _ => throw new InvalidOperationException("Unsupported sample engine: " + engine),
    };

    private static string RequireConnectionString(IConfiguration configuration, string? configuredEngine)
    {
        var name = SampleEngineParser.ConnectionStringName(configuredEngine);
        var connectionString = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string '" + name + "' is not configured. " +
                "Set it via user secrets: dotnet user-secrets set \"ConnectionStrings:" + name +
                "\" \"<connection-string>\" --project samples/Expresso.Sample.WebApi");
        }

        return connectionString;
    }
}
