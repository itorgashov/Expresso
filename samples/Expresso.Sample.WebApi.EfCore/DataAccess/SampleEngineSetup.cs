using Expresso.Rendering.EntityFrameworkCore;
using IBM.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Expresso.Sample.WebApi.EfCore.DataAccess;

public static class SampleEngineSetup
{
    public static void AddSampleEngine(IServiceCollection services, IConfiguration configuration)
    {
        var configuredEngine = configuration["ExpressoSample:Engine"];
        var engine = SampleEngineParser.Parse(configuredEngine);
        var mariaDb = SampleEngineParser.IsMariaDb(configuredEngine);
        var connectionString = ConnectionStringAdjustments.ForEngine(
            engine,
            RequireConnectionString(configuration, configuredEngine),
            mariaDb);

        Action<DbContextOptionsBuilder> configure = engine switch
        {
            SampleEngine.SqlServer => b => b.UseSqlServer(connectionString),
            SampleEngine.PostgreSql => b => b.UseNpgsql(connectionString),
            SampleEngine.MySql => b => b.UseMySql(connectionString, MySqlServerVersion(connectionString)),
            SampleEngine.Sqlite => b => b.UseSqlite(connectionString),
            SampleEngine.Oracle => b => b.UseOracle(connectionString),
            SampleEngine.Db2 => b => b.UseDb2(connectionString, _ => { }),
            _ => throw new InvalidOperationException("Unsupported sample engine: " + engine),
        };

        services.AddDbContext<SampleDbContext>(configure);
        services.AddEfCoreExpressionTransformations<SampleDbContext>();

        services.AddScoped<IRepository<Entities.Book>, BookRepository>();
        services.AddScoped<IRepository<Entities.Author>, AuthorRepository>();
        services.AddScoped<IRepository<Entities.Publisher>, PublisherRepository>();
    }

    private static ServerVersion MySqlServerVersion(string connectionString)
    {
        using var connection = new MySqlConnection(connectionString);
        connection.Open();
        return ServerVersion.AutoDetect(connection);
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
}
