using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Expresso.Sample.Shared.DataAccess;

public interface ISampleDb
{
    ISampleSql Sql { get; }

    Task<DbConnection> OpenAsync(CancellationToken cancellationToken = default);

    void Bind(DbCommand command, string name, object? value);

    void BindAll(DbCommand command, System.Collections.Generic.Dictionary<string, object>? parameters);
}
