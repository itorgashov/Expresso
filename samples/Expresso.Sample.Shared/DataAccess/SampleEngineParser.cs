using System;

namespace Expresso.Sample.Shared.DataAccess;

public static class SampleEngineParser
{
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
    /// Configuration key under <c>ConnectionStrings</c>. <c>MariaDb</c> is its own database
    /// (the MySQL renderer is selected separately by <see cref="Parse"/>).
    /// </summary>
    public static string ConnectionStringName(string? configuredEngine)
    {
        if (string.IsNullOrWhiteSpace(configuredEngine))
        {
            return nameof(SampleEngine.SqlServer);
        }

        switch (configuredEngine.Trim().ToLowerInvariant())
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
