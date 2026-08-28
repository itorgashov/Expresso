using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace Expresso.Sample.Shared.DataAccess;

internal static class SqlParameterExtensions
{
    public static void AddParameters(this SqlCommand command, Dictionary<string, object>? parameters)
    {
        if (parameters is null)
        {
            return;
        }

        foreach (var pair in parameters)
        {
            command.Parameters.AddWithValue(pair.Key, pair.Value ?? System.DBNull.Value);
        }
    }

    public static void MergeParameters(Dictionary<string, object> target, IReadOnlyDictionary<string, object> source)
    {
        foreach (var pair in source)
        {
            if (!target.ContainsKey(pair.Key))
            {
                target.Add(pair.Key, pair.Value);
            }
        }
    }
}
