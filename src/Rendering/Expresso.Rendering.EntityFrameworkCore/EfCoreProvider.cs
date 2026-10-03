namespace Expresso.Rendering.EntityFrameworkCore
{
    /// <summary>Database engine behind an EF Core provider; selects the Expresso overrides.</summary>
    public enum EfCoreProvider
    {
        /// <summary>Unknown provider: no overrides, native EF translation only.</summary>
        Other = 0,

        /// <summary><c>Microsoft.EntityFrameworkCore.SqlServer</c>.</summary>
        SqlServer,

        /// <summary><c>Npgsql.EntityFrameworkCore.PostgreSQL</c>.</summary>
        PostgreSql,

        /// <summary><c>Pomelo.EntityFrameworkCore.MySql</c> or <c>MySql.EntityFrameworkCore</c> (MySQL and MariaDB).</summary>
        MySql,

        /// <summary><c>Microsoft.EntityFrameworkCore.Sqlite</c>.</summary>
        Sqlite,

        /// <summary><c>Oracle.EntityFrameworkCore</c>.</summary>
        Oracle,

        /// <summary><c>IBM.EntityFrameworkCore</c>.</summary>
        Db2,
    }

    /// <summary>Maps <c>DbContext.Database.ProviderName</c> to <see cref="EfCoreProvider"/>.</summary>
    public static class EfCoreProviders
    {
        /// <summary>Provider for <paramref name="providerName"/>; <see cref="EfCoreProvider.Other"/> when unknown or <see langword="null"/>.</summary>
        public static EfCoreProvider Resolve(string? providerName) => providerName switch
        {
            "Microsoft.EntityFrameworkCore.SqlServer" => EfCoreProvider.SqlServer,
            "Npgsql.EntityFrameworkCore.PostgreSQL" => EfCoreProvider.PostgreSql,
            "Pomelo.EntityFrameworkCore.MySql" or "MySql.EntityFrameworkCore" => EfCoreProvider.MySql,
            "Microsoft.EntityFrameworkCore.Sqlite" => EfCoreProvider.Sqlite,
            "Oracle.EntityFrameworkCore" => EfCoreProvider.Oracle,
            "IBM.EntityFrameworkCore" => EfCoreProvider.Db2,
            _ => EfCoreProvider.Other,
        };
    }
}
