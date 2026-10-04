namespace Expresso.Sample.WebApi.EfCore.DataAccess;

/// <summary>Reads <c>ExpressoSample:Engine</c> and the matching connection-string name.</summary>
public static class SampleEngineParser
{
    public static SampleEngine Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return SampleEngine.SqlServer;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "sqlserver" or "mssql" => SampleEngine.SqlServer,
            "postgresql" or "postgres" => SampleEngine.PostgreSql,
            "mysql" or "mariadb" => SampleEngine.MySql,
            "sqlite" => SampleEngine.Sqlite,
            "oracle" => SampleEngine.Oracle,
            "db2" => SampleEngine.Db2,
            _ => throw new InvalidOperationException(
                "Unknown ExpressoSample:Engine '" + value + "'. Use SqlServer, PostgreSql, MySql, MariaDb, Sqlite, Oracle, or Db2."),
        };
    }

    public static string ConnectionStringName(string? configuredEngine)
    {
        if (string.IsNullOrWhiteSpace(configuredEngine))
        {
            return nameof(SampleEngine.SqlServer);
        }

        return configuredEngine.Trim().ToLowerInvariant() switch
        {
            "sqlserver" or "mssql" => nameof(SampleEngine.SqlServer),
            "postgresql" or "postgres" => nameof(SampleEngine.PostgreSql),
            "mysql" => nameof(SampleEngine.MySql),
            "mariadb" => "MariaDb",
            "sqlite" => nameof(SampleEngine.Sqlite),
            "oracle" => nameof(SampleEngine.Oracle),
            "db2" => nameof(SampleEngine.Db2),
            _ => throw new InvalidOperationException(
                "Unknown ExpressoSample:Engine '" + configuredEngine + "'. Use SqlServer, PostgreSql, MySql, MariaDb, Sqlite, Oracle, or Db2."),
        };
    }

    public static bool IsMariaDb(string? configuredEngine) =>
        string.Equals(configuredEngine?.Trim(), "MariaDb", StringComparison.OrdinalIgnoreCase);
}
