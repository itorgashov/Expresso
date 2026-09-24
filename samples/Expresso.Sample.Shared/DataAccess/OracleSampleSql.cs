namespace Expresso.Sample.Shared.DataAccess;

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
