namespace Expresso.Sample.Shared.DataAccess;

/// <summary>MySQL and MariaDB fragments. Identifiers are left unquoted.</summary>
public sealed class MySqlSampleSql : SampleSqlBase
{
    /// <inheritdoc />
    public override SampleEngine Engine => SampleEngine.MySql;

    /// <inheritdoc />
    public override string Table(string name) => name;
}
