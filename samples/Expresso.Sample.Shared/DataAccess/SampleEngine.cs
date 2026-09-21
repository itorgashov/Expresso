using System;

namespace Expresso.Sample.Shared.DataAccess;

public enum SampleEngine
{
    SqlServer,
    PostgreSql,
    MySql,
    Sqlite,
    Oracle,
    Db2
}

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
                    "Unknown ExpressoSample:Engine '" + value + "'. Use SqlServer, PostgreSql, MySql, Sqlite, Oracle, or Db2.");
        }
    }

    public static string ConnectionStringName(SampleEngine engine) => engine.ToString();
}
