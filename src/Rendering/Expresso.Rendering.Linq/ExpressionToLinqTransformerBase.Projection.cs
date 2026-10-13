using Expresso.Core.Projection;

namespace Expresso.Rendering.Linq;

public abstract partial class ExpressionToLinqTransformerBase : IProjectionToLinqTransformer
{
    /// <inheritdoc />
    public LinqProjectionPlan<TEntity, TResult> RenderProjection<TEntity, TResult>(
        ProjectionDirective projection, LinqProjectionMapping<TEntity, TResult> mapping) where TResult : new() =>
        new LinqProjectionPlan<TEntity, TResult>(projection, mapping, this as IProjectionEntitySourceProvider);
}
