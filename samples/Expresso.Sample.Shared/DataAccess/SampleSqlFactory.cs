using System;

namespace Expresso.Sample.Shared.DataAccess;

public static class SampleSqlFactory
{
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
