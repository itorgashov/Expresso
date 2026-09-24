namespace Expresso.Sample.Shared.DataAccess;

/// <summary>SQL Server fragments. Tables are qualified with <c>dbo</c>.</summary>
public sealed class SqlServerSampleSql : SampleSqlBase
{
    /// <inheritdoc />
    public override SampleEngine Engine => SampleEngine.SqlServer;

    /// <summary>Returns <c>dbo.</c> plus <paramref name="name"/>.</summary>
    /// <param name="name">Unqualified table name.</param>
    /// <returns>The schema-qualified table name.</returns>
    public override string Table(string name) => "dbo." + name;
}
