using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Runs <c>SELECT COUNT(*)</c> and reads the scalar as a 64-bit count.</summary>
internal static class SampleCount
{
    public static async Task<long> ReadAsync(
        ISampleDb db,
        string sql,
        Dictionary<string, object>? parameters,
        CancellationToken cancellationToken)
    {
        var connection = await db.OpenAsync(cancellationToken);
        using (connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                db.BindAll(command, parameters);
                var value = await command.ExecuteScalarAsync(cancellationToken);
                if (value is null || value is DBNull)
                {
                    return 0;
                }

                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
        }
    }
}
