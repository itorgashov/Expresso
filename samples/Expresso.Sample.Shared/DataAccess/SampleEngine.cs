namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Database engine the sample host talks to.</summary>
public enum SampleEngine
{
    /// <summary>Microsoft SQL Server.</summary>
    SqlServer,

    /// <summary>PostgreSQL.</summary>
    PostgreSql,

    /// <summary>MySQL or MariaDB. MariaDB uses this renderer and its own connection string.</summary>
    MySql,

    /// <summary>SQLite.</summary>
    Sqlite,

    /// <summary>Oracle.</summary>
    Oracle,

    /// <summary>IBM Db2.</summary>
    Db2
}
