using System;

namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Reads <c>ExpressoSample:Engine</c> and the matching connection-string name.</summary>
public static class SampleEngineParser
{
    /// <summary>Maps a configuration value to the renderer engine. <c>MariaDb</c> uses <see cref="SampleEngine.MySql"/>.</summary>
    /// <param name="value">Raw <c>ExpressoSample:Engine</c> value. Blank selects SQL Server.</param>
    /// <returns>The engine whose SQL catalog and transformer the host should register.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="value"/> is not a known engine name.</exception>
    public static SampleEngine Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return SampleEngine.SqlServer;
        }

        switch (value!.Trim().ToLowerInvariant())
        {
            case "sqlserver":
            case "mssql":
                return SampleEngine.SqlServer;
            case "postgresql":
            case "postgres":
                return SampleEngine.PostgreSql;
            case "mysql":
            case "mariadb":
                return SampleEngine.MySql;
            case "sqlite":
                return SampleEngine.Sqlite;
            case "oracle":
                return SampleEngine.Oracle;
            case "db2":
                return SampleEngine.Db2;
            default:
                throw new InvalidOperationException(
                    "Unknown ExpressoSample:Engine '" + value + "'. Use SqlServer, PostgreSql, MySql, MariaDb, Sqlite, Oracle, or Db2.");
        }
    }

    /// <summary>
    /// Returns the <c>ConnectionStrings</c> key for <paramref name="configuredEngine"/>.
    /// <c>MariaDb</c> is its own database; <see cref="Parse"/> still selects the MySQL renderer.
    /// </summary>
    /// <param name="configuredEngine">Raw <c>ExpressoSample:Engine</c> value. Blank selects <c>SqlServer</c>.</param>
    /// <returns>The connection-string name, such as <c>MariaDb</c> or <c>SqlServer</c>.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="configuredEngine"/> is not a known engine name.</exception>
    public static string ConnectionStringName(string? configuredEngine)
    {
        if (string.IsNullOrWhiteSpace(configuredEngine))
        {
            return nameof(SampleEngine.SqlServer);
        }

        var engineName = configuredEngine!.Trim();
        switch (engineName.ToLowerInvariant())
        {
            case "sqlserver":
            case "mssql":
                return nameof(SampleEngine.SqlServer);
            case "postgresql":
            case "postgres":
                return nameof(SampleEngine.PostgreSql);
            case "mysql":
                return nameof(SampleEngine.MySql);
            case "mariadb":
                return "MariaDb";
            case "sqlite":
                return nameof(SampleEngine.Sqlite);
            case "oracle":
                return nameof(SampleEngine.Oracle);
            case "db2":
                return nameof(SampleEngine.Db2);
            default:
                throw new InvalidOperationException(
                    "Unknown ExpressoSample:Engine '" + configuredEngine + "'. Use SqlServer, PostgreSql, MySql, MariaDb, Sqlite, Oracle, or Db2.");
        }
    }
}
