using System;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Sample.Shared.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Expresso.Sample.WebApi.NetFx.DataAccess;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly IConfiguration _configuration;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("ExpressoSample");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'ExpressoSample' is not configured. " +
                "Set it via user secrets: dotnet user-secrets set \"ConnectionStrings:ExpressoSample\" \"<connection-string>\" --project samples/Expresso.Sample.WebApi.NetFx");
        }

        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
