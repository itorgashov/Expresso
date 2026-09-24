using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Opens sample-database connections and binds parameters for the active engine.</summary>
public interface ISampleDb
{
    /// <summary>SQL fragments for the engine this connection talks to.</summary>
    ISampleSql Sql { get; }

    /// <summary>Opens a new connection.</summary>
    /// <param name="cancellationToken">Token that cancels opening the connection.</param>
    /// <returns>An open connection. The caller disposes it.</returns>
    Task<DbConnection> OpenAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds one parameter to <paramref name="command"/>.</summary>
    /// <param name="command">Command that will execute.</param>
    /// <param name="name">Parameter name, with or without a <c>@</c> or <c>:</c> prefix.</param>
    /// <param name="value">Value to bind. <see langword="null"/> is stored as a database null.</param>
    void Bind(DbCommand command, string name, object? value);

    /// <summary>Adds every entry in <paramref name="parameters"/> to <paramref name="command"/>.</summary>
    /// <param name="command">Command that will execute.</param>
    /// <param name="parameters">Parameters to bind, or <see langword="null"/> to bind nothing.</param>
    void BindAll(DbCommand command, System.Collections.Generic.Dictionary<string, object>? parameters);
}
