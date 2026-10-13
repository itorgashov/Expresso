using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

internal static partial class PublisherTimes
{
    internal static async Task<Dictionary<int, Dictionary<string, object?>>> ReadSelectedAsync(
        SampleEf6Context db, IReadOnlyList<int> ids, bool opens, bool closes, CancellationToken cancellationToken)
    {
        var result = new Dictionary<int, Dictionary<string, object?>>();
        var connection = db.Database.Connection;
        var opened = connection.State != ConnectionState.Open;
        if (opened) await connection.OpenAsync(cancellationToken);
        try
        {
            for (var start = 0; start < ids.Count; start += 500)
            {
                using var command = connection.CreateCommand();
                command.Transaction = db.Database.CurrentTransaction?.UnderlyingTransaction;
                var names = new List<string>();
                foreach (var id in ids.Skip(start).Take(500))
                {
                    var name = "p" + names.Count;
                    names.Add(":" + name);
                    command.Parameters.Add(new OracleParameter(name, id));
                }
                command.CommandText = OracleSelect(string.Join(",", names), opens, closes);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var values = new Dictionary<string, object?>();
                    var ordinal = 1;
                    if (opens) values.Add("opensAt", ReadClock(reader, ordinal++));
                    if (closes) values.Add("closesAt", ReadClock(reader, ordinal));
                    result.Add(Convert.ToInt32(reader.GetValue(0)), values);
                }
            }
        }
        finally { if (opened) connection.Close(); }
        return result;
    }
    internal static object? ReadClock(System.Data.Common.DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : ParseTime(reader.GetString(ordinal));
}
