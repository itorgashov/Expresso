namespace Expresso.Sample.Shared.DataAccess;

public sealed class SqliteSampleSql : SampleSqlBase
{
    public override SampleEngine Engine => SampleEngine.Sqlite;

    public override string Table(string name) => name;
}
