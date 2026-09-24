using System;
using System.Data;
using System.Data.Common;

namespace Expresso.Sample.Shared.DataAccess;

public sealed class AtParameterBinder : ISampleParameterBinder
{
    public void Bind(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        var trimmed = name.TrimStart('@', ':');
        parameter.ParameterName = name.StartsWith(":", StringComparison.Ordinal) ? ":" + trimmed : "@" + trimmed;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
