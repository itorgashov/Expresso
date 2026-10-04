using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.Entity.SqlServer;
using System.Data.SQLite;
using System.Data.SQLite.EF6;
using MySql.Data.EntityFramework;
using MySql.Data.MySqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.EntityFramework;

namespace Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

public sealed class SampleEf6Configuration : DbConfiguration
{
    public SampleEf6Configuration()
    {
        SetProviderServices(SqlProviderServices.ProviderInvariantName, SqlProviderServices.Instance);

        SetProviderFactory("Npgsql", NpgsqlFactory.Instance);
        SetProviderServices("Npgsql", NpgsqlServices.Instance);

        SetProviderFactory("MySql.Data.MySqlClient", new MySqlClientFactory());
        SetProviderServices("MySql.Data.MySqlClient", new MySqlProviderServices());

        var sqliteServices = (DbProviderServices)SQLiteProviderFactory.Instance.GetService(typeof(DbProviderServices));
        SetProviderFactory("System.Data.SQLite", SQLiteFactory.Instance);
        SetProviderFactory("System.Data.SQLite.EF6", SQLiteProviderFactory.Instance);
        SetProviderServices("System.Data.SQLite", sqliteServices);
        SetProviderServices("System.Data.SQLite.EF6", sqliteServices);

        SetProviderFactory("Oracle.ManagedDataAccess.Client", OracleClientFactory.Instance);
        SetProviderServices("Oracle.ManagedDataAccess.Client", EFOracleProviderServices.Instance);

        SetDatabaseInitializer<SampleEf6Context>(null);
    }
}
