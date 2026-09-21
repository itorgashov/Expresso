using System.Data.Common;

namespace Expresso.Sample.Shared.DataAccess;

public interface ISampleParameterBinder
{
    void Bind(DbCommand command, string name, object? value);
}
