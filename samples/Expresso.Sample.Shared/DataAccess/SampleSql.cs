namespace Expresso.Sample.Shared.DataAccess;

public abstract class SampleSqlBase : ISampleSql
{
    public abstract SampleEngine Engine { get; }

    public abstract string Table(string name);

    public virtual string TableAs(string name, string alias) => Table(name) + " AS " + alias;

    public virtual string Col(string tableAlias, string columnName) => tableAlias + "." + columnName;

    public virtual string Param(string name) => "@" + name;
}

public sealed class SqlServerSampleSql : SampleSqlBase
{
    public override SampleEngine Engine => SampleEngine.SqlServer;

    public override string Table(string name) => "dbo." + name;
}

public sealed class PostgreSqlSampleSql : SampleSqlBase
{
    public override SampleEngine Engine => SampleEngine.PostgreSql;

    public override string Table(string name) => name;
}

public sealed class MySqlSampleSql : SampleSqlBase
{
    public override SampleEngine Engine => SampleEngine.MySql;

    public override string Table(string name) => name;
}

public sealed class SqliteSampleSql : SampleSqlBase
{
    public override SampleEngine Engine => SampleEngine.Sqlite;

    public override string Table(string name) => name;
}

public sealed class OracleSampleSql : SampleSqlBase
{
    public override SampleEngine Engine => SampleEngine.Oracle;

    public override string Table(string name) => "\"" + name + "\"";

    // Oracle does not allow AS for table aliases.
    public override string TableAs(string name, string alias) => Table(name) + " " + alias;

    public override string Col(string tableAlias, string columnName) =>
        tableAlias + ".\"" + columnName + "\"";

    public override string Param(string name) => ":" + name;
}

public sealed class Db2SampleSql : SampleSqlBase
{
    public override SampleEngine Engine => SampleEngine.Db2;

    public override string Table(string name) => "\"" + name + "\"";

    public override string Col(string tableAlias, string columnName) =>
        tableAlias + ".\"" + columnName + "\"";
}

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
            _ => throw new System.ArgumentOutOfRangeException(nameof(engine), engine, null)
        };
}
