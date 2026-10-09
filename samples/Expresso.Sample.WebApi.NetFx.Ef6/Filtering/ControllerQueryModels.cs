using Expresso.Core.Filtering;

namespace Expresso.Sample.WebApi.NetFx.Ef6.Filtering;

/// <summary>Startup-compiled query catalogs for one controller.</summary>
/// <typeparam name="TController">The controller that owns these catalogs.</typeparam>
public sealed class ControllerQueryModels<TController>
{
    /// <summary>Creates a typed holder for the endpoint's models.</summary>
    public ControllerQueryModels(QueryModel filter, QueryModel sort) { Filter = filter; Sort = sort; }
    /// <summary>The filter model, optionally carrying a compiled policy.</summary>
    public QueryModel Filter { get; }
    /// <summary>The sort model, optionally carrying a compiled policy.</summary>
    public QueryModel Sort { get; }
}
