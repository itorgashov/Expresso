using System;

namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Creates the <see cref="ISampleSql"/> implementation for a sample engine.</summary>
public static class SampleSqlFactory
{
    /// <summary>Returns the SQL catalog for <paramref name="engine"/>.</summary>
    /// <param name="engine">Engine selected by the host.</param>
    /// <returns>The dialect catalog.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="engine"/> is not a known sample engine.</exception>
    public static ISampleSql Create(SampleEngine engine) =>
        engine switch
        {
            SampleEngine.SqlServer => new SqlServerSampleSql(),
            SampleEngine.PostgreSql => new PostgreSqlSampleSql(),
            SampleEngine.MySql => new MySqlSampleSql(),
            SampleEngine.Sqlite => new SqliteSampleSql(),
            SampleEngine.Oracle => new OracleSampleSql(),
            SampleEngine.Db2 => new Db2SampleSql(),
            _ => throw new ArgumentOutOfRangeException(nameof(engine), engine, null)
        };
}
