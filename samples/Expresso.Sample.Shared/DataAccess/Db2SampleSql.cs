namespace Expresso.Sample.Shared.DataAccess;

public sealed class Db2SampleSql : SampleSqlBase
{
    public override SampleEngine Engine => SampleEngine.Db2;

    public override string Table(string name) => "\"" + name + "\"";

    public override string Col(string tableAlias, string columnName) =>
        tableAlias + ".\"" + columnName + "\"";
}
