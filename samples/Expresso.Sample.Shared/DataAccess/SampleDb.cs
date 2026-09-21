using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Expresso.Sample.Shared.DataAccess;

public sealed class SampleDb : ISampleDb
{
    private readonly System.Func<DbConnection> _createConnection;
    private readonly ISampleParameterBinder _binder;

    public SampleDb(ISampleSql sql, System.Func<DbConnection> createConnection, ISampleParameterBinder binder)
    {
        Sql = sql;
        _createConnection = createConnection;
        _binder = binder;
    }

    public ISampleSql Sql { get; }

    public async Task<DbConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        var connection = _createConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    public void Bind(DbCommand command, string name, object? value) =>
        _binder.Bind(command, name, Coerce(value));

    public void BindAll(DbCommand command, Dictionary<string, object>? parameters)
    {
        if (parameters is null)
        {
            return;
        }

        foreach (var pair in parameters)
        {
            Bind(command, pair.Key, pair.Value);
        }
    }

    private object? Coerce(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is System.Guid guid &&
            (Sql.Engine == SampleEngine.MySql || Sql.Engine == SampleEngine.Sqlite || Sql.Engine == SampleEngine.Db2))
        {
            return guid.ToString();
        }

        return value;
    }
}
