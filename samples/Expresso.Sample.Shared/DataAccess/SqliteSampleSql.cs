namespace Expresso.Sample.Shared.DataAccess;

/// <summary>SQLite fragments. Identifiers are left unquoted.</summary>
public sealed class SqliteSampleSql : SampleSqlBase
{
    /// <inheritdoc />
    public override SampleEngine Engine => SampleEngine.Sqlite;

    /// <inheritdoc />
    public override string Table(string name) => name;
}
