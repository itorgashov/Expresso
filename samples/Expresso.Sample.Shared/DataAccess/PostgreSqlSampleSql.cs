namespace Expresso.Sample.Shared.DataAccess;

public sealed class PostgreSqlSampleSql : SampleSqlBase
{
    public override SampleEngine Engine => SampleEngine.PostgreSql;

    public override string Table(string name) => name;
}
