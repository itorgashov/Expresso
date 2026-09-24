using System.Data.Common;

namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Writes one parameter onto an ADO.NET command for a sample engine.</summary>
public interface ISampleParameterBinder
{
    /// <summary>Adds one parameter to <paramref name="command"/>.</summary>
    /// <param name="command">Command that will execute.</param>
    /// <param name="name">Parameter name, with or without a <c>@</c> or <c>:</c> prefix.</param>
    /// <param name="value">Value to bind. <see langword="null"/> is stored as a database null.</param>
    void Bind(DbCommand command, string name, object? value);
}
