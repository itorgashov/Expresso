namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Db2 fragments. Table and column names are quoted lowercase.</summary>
public sealed class Db2SampleSql : SampleSqlBase
{
    /// <inheritdoc />
    public override SampleEngine Engine => SampleEngine.Db2;

    /// <summary>Returns the table name in double quotes so Db2 keeps it lowercase.</summary>
    /// <param name="name">Unqualified table name.</param>
    /// <returns>The quoted table name.</returns>
    public override string Table(string name) => "\"" + name + "\"";

    /// <summary>Returns <paramref name="tableAlias"/> and a quoted lowercase column name.</summary>
    /// <param name="tableAlias">Table alias already present in the query.</param>
    /// <param name="columnName">Unquoted column name.</param>
    /// <returns>The qualified, quoted column.</returns>
    public override string Col(string tableAlias, string columnName) =>
        tableAlias + ".\"" + columnName + "\"";
}
