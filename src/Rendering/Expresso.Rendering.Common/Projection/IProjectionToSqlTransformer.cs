using Expresso.Core.Projection;

namespace Expresso.Rendering;

/// <summary>Renders validated projections as minimal SQL select lists and requested child plans.</summary>
public interface IProjectionToSqlTransformer
{
    /// <summary>Renders application-owned column expressions. Client paths never become SQL.</summary>
    RenderedSqlProjection RenderProjection(ProjectionDirective projection, SqlProjectionMapping mapping);
    /// <summary>Renders a parameterized structural key predicate with SELECT-compatible expression quoting.</summary>
    (string whereClause, Dictionary<string, object> parameters) RenderProjectionKeys(
        IReadOnlyList<SqlProjectionKey> components, IReadOnlyList<CompositeProjectionKey> keys, string paramNamePrefix = "key");
}
