using Expresso.Core.Filtering;

namespace Expresso.Core.Projection;

/// <summary>Supplies the public projection allow-list independently of filter and sort policy.</summary>
public interface IRequestProjectionModelProvider
{
    /// <summary>Gets the projection catalog for an endpoint context.</summary>
    QueryModel GetProjectionModel(string context);
}
