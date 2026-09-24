using System;
using System.Data;
using System.Data.Common;

namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Binds parameters with an <c>@</c> name, or keeps a leading <c>:</c> when the caller already used one.</summary>
public sealed class AtParameterBinder : ISampleParameterBinder
{
    /// <inheritdoc />
    public void Bind(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        var trimmed = name.TrimStart('@', ':');
        parameter.ParameterName = name.StartsWith(":", StringComparison.Ordinal) ? ":" + trimmed : "@" + trimmed;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
