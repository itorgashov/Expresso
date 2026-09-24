namespace Expresso.Sample.Shared.DataAccess;

/// <summary>PostgreSQL fragments. Identifiers are left unquoted.</summary>
public sealed class PostgreSqlSampleSql : SampleSqlBase
{
    /// <inheritdoc />
    public override SampleEngine Engine => SampleEngine.PostgreSql;

    /// <inheritdoc />
    public override string Table(string name) => name;
}
