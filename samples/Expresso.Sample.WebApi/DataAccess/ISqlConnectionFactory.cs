using Microsoft.Data.SqlClient;

namespace Expresso.Sample.WebApi.DataAccess;

public interface ISqlConnectionFactory
{
    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
