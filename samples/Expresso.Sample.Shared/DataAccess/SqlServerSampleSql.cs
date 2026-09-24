namespace Expresso.Sample.Shared.DataAccess;

public sealed class SqlServerSampleSql : SampleSqlBase
{
    public override SampleEngine Engine => SampleEngine.SqlServer;

    public override string Table(string name) => "dbo." + name;
}
