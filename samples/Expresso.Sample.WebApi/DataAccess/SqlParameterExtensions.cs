using Microsoft.Data.SqlClient;

namespace Expresso.Sample.WebApi.DataAccess;

internal static class SqlParameterExtensions
{
    public static void AddParameters(this SqlCommand command, Dictionary<string, object>? parameters)
    {
        if (parameters is null)
        {
            return;
        }

        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
    }
}
