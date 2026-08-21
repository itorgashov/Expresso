using Microsoft.Data.SqlClient;

namespace Expresso.Sample.WebApi.DataAccess;

public sealed class SqlConnectionFactory(IConfiguration configuration) : ISqlConnectionFactory
{
    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("ExpressoSample");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'ExpressoSample' is not configured. " +
                "Set it via user secrets: dotnet user-secrets set \"ConnectionStrings:ExpressoSample\" \"<connection-string>\"");
        }

        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
