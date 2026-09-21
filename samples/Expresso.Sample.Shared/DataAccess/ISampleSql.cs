namespace Expresso.Sample.Shared.DataAccess;

public interface ISampleSql
{
    SampleEngine Engine { get; }

    string Table(string name);

    string TableAs(string name, string alias);

    /// <summary>Qualified column reference (e.g. a.created_at). Oracle/Db2 quote the column name.</summary>
    string Col(string tableAlias, string columnName);

    string Param(string name);
}
