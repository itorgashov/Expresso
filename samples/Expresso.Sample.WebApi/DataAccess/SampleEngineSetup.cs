using System.Data.Common;
using Expresso.Sample.Shared.DataAccess;
using Expresso.Sample.Shared.Models;
using Expresso.Rendering;
using IBM.Data.Db2;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;

namespace Expresso.Sample.WebApi.DataAccess;

/// <summary>Registers the sample engine, its ADO.NET driver, and the shared repositories.</summary>
public static class SampleEngineSetup
{
    /// <summary>
    /// Registers the transformer, connection, and repositories for <c>ExpressoSample:Engine</c>.
    /// The connection string is <c>ConnectionStrings:{Engine}</c>; <c>MariaDb</c> uses <c>ConnectionStrings:MariaDb</c>.
    /// </summary>
    /// <param name="services">Application service collection.</param>
    /// <param name="configuration">Host configuration.</param>
    /// <exception cref="InvalidOperationException">The engine name or its connection string is missing.</exception>
    public static void AddSampleEngine(IServiceCollection services, IConfiguration configuration)
    {
        var configuredEngine = configuration["ExpressoSample:Engine"];
        var engine = SampleEngineParser.Parse(configuredEngine);
        var connectionString = RequireConnectionString(configuration, configuredEngine);

        services.AddSingleton<ISampleSql>(SampleSqlFactory.Create(engine));

        switch (engine)
        {
            case SampleEngine.SqlServer:
                services.AddSqlServerExpressionTransformations();
                services.AddSingleton<ISampleParameterBinder, AtParameterBinder>();
                RegisterDb(services, () => new SqlConnection(connectionString));
                break;
            case SampleEngine.PostgreSql:
                services.AddPostgreSqlExpressionTransformations();
                services.AddSingleton<ISampleParameterBinder, AtParameterBinder>();
                RegisterDb(services, () => new NpgsqlConnection(connectionString));
                break;
            case SampleEngine.MySql:
                services.AddMySqlExpressionTransformations();
                services.AddSingleton<ISampleParameterBinder, AtParameterBinder>();
                RegisterDb(services, () => new MySqlConnection(connectionString));
                break;
            case SampleEngine.Sqlite:
                services.AddSqliteExpressionTransformations();
                services.AddSingleton<ISampleParameterBinder, AtParameterBinder>();
                RegisterDb(services, () => new SqliteConnection(connectionString));
                break;
            case SampleEngine.Oracle:
                services.AddOracleExpressionTransformations();
                services.AddSingleton<ISampleParameterBinder, OracleStyleParameterBinder>();
                RegisterDb(services, () => new OracleConnection(connectionString));
                break;
            case SampleEngine.Db2:
                services.AddDb2ExpressionTransformations();
                services.AddSingleton<ISampleParameterBinder, AtParameterBinder>();
                RegisterDb(services, () => new DB2Connection(connectionString));
                break;
            default:
                throw new InvalidOperationException("Unsupported sample engine: " + engine);
        }

        services.AddScoped<IRepository<Book>, BookRepository>();
        services.AddScoped<IRepository<Author>, AuthorRepository>();
        services.AddScoped<IRepository<Publisher>, PublisherRepository>();
    }

    private static string RequireConnectionString(IConfiguration configuration, string? configuredEngine)
    {
        var name = SampleEngineParser.ConnectionStringName(configuredEngine);
        var connectionString = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string '" + name + "' is not configured. " +
                "Set it via user secrets: dotnet user-secrets set \"ConnectionStrings:" + name + "\" \"<connection-string>\"");
        }

        return connectionString;
    }

    private static void RegisterDb(IServiceCollection services, Func<DbConnection> create) =>
        services.AddSingleton<ISampleDb>(sp => new SampleDb(
            sp.GetRequiredService<ISampleSql>(),
            create,
            sp.GetRequiredService<ISampleParameterBinder>()));
}
