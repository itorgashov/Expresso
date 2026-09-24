namespace Expresso.Sample.Shared.DataAccess;

public abstract class SampleSqlBase : ISampleSql
{
    public abstract SampleEngine Engine { get; }

    public abstract string Table(string name);

    public virtual string TableAs(string name, string alias) => Table(name) + " AS " + alias;

    public virtual string Col(string tableAlias, string columnName) => tableAlias + "." + columnName;

    public virtual string Param(string name) => "@" + name;
}
