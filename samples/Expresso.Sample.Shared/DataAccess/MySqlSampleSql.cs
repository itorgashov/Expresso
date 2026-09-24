namespace Expresso.Sample.Shared.DataAccess;

public sealed class MySqlSampleSql : SampleSqlBase
{
    public override SampleEngine Engine => SampleEngine.MySql;

    public override string Table(string name) => name;
}
