using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Infrastructure.DependencyResolution;

namespace Expresso.Rendering.EntityFramework
{
    /// <summary>EF6 provider families whose translation can differ from the Expresso SQL renderers.</summary>
    public enum Ef6Provider
    {
        /// <summary>Unknown provider: no overrides beyond the portable ones.</summary>
        Other = 0,

        /// <summary><c>System.Data.SqlClient</c> / <c>Microsoft.Data.SqlClient</c>.</summary>
        SqlServer,

        /// <summary><c>Npgsql</c>.</summary>
        PostgreSql,

        /// <summary><c>MySql.Data.MySqlClient</c> (MySQL and MariaDB).</summary>
        MySql,

        /// <summary><c>System.Data.SQLite.EF6</c>.</summary>
        Sqlite,

        /// <summary><c>Oracle.ManagedDataAccess.Client</c>.</summary>
        Oracle,
    }

    /// <summary>Maps EF6 provider invariant names to <see cref="Ef6Provider"/>.</summary>
    public static class Ef6Providers
    {
        /// <summary>Provider for an ADO.NET provider invariant name.</summary>
        public static Ef6Provider Resolve(string? providerInvariantName) => providerInvariantName switch
        {
            "System.Data.SqlClient" or "Microsoft.Data.SqlClient" => Ef6Provider.SqlServer,
            "Npgsql" => Ef6Provider.PostgreSql,
            "MySql.Data.MySqlClient" => Ef6Provider.MySql,
            "System.Data.SQLite.EF6" or "System.Data.SQLite" => Ef6Provider.Sqlite,
            "Oracle.ManagedDataAccess.Client" => Ef6Provider.Oracle,
            _ => Ef6Provider.Other,
        };

        /// <summary>Provider of the connection's ADO.NET factory, as registered with EF6.</summary>
        public static Ef6Provider Resolve(DbConnection connection)
        {
            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var factory = DbProviderFactories.GetFactory(connection);
            return Resolve(DbConfiguration.DependencyResolver.GetService<IProviderInvariantName>(factory)?.Name);
        }
    }
}
