namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Oracle fragments. Table and column names are quoted lowercase, and parameters use <c>:</c>.</summary>
public sealed class OracleSampleSql : SampleSqlBase
{
    /// <inheritdoc />
    public override SampleEngine Engine => SampleEngine.Oracle;

    /// <summary>Returns the table name in double quotes so Oracle keeps it lowercase.</summary>
    /// <param name="name">Unqualified table name.</param>
    /// <returns>The quoted table name.</returns>
    public override string Table(string name) => "\"" + name + "\"";

    /// <summary>Returns the quoted table and alias. Oracle does not allow <c>AS</c> for table aliases.</summary>
    /// <param name="name">Unqualified table name.</param>
    /// <param name="alias">Alias used in the query.</param>
    /// <returns>The quoted table followed by the alias.</returns>
    public override string TableAs(string name, string alias) => Table(name) + " " + alias;

    /// <summary>Returns <paramref name="tableAlias"/> and a quoted lowercase column name.</summary>
    /// <param name="tableAlias">Table alias already present in the query.</param>
    /// <param name="columnName">Unquoted column name.</param>
    /// <returns>The qualified, quoted column.</returns>
    public override string Col(string tableAlias, string columnName) =>
        tableAlias + ".\"" + columnName + "\"";

    /// <summary>Returns a colon bind marker.</summary>
    /// <param name="name">Parameter name without a prefix.</param>
    /// <returns><c>:</c> followed by <paramref name="name"/>.</returns>
    public override string Param(string name) => ":" + name;
}
