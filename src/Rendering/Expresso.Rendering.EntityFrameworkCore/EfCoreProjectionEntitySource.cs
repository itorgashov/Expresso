using System.Linq.Expressions;
using Expresso.Rendering.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Expresso.Rendering.EntityFrameworkCore;

public partial class EfCoreExpressionToLinqTransformer : IProjectionEntitySourceProvider, IProjectionSourceValidator
{
    /// <inheritdoc />
    IQueryable? IProjectionEntitySourceProvider.GetEntitySource(IQueryable source, Type entityType) =>
        EfCoreProjectionEntitySource.Resolve(source, entityType);

    /// <inheritdoc />
    public void ValidateSource(IQueryable source) => EfCoreProjectionSourceValidator.Validate(source);
}

internal static class EfCoreProjectionEntitySource
{
    internal static IQueryable? Resolve(IQueryable source, Type entityType)
    {
        if (source.Provider is not IAsyncQueryProvider provider) return null;
        EfCoreProjectionSourceValidator.Validate(source);
        var roots = new FindModel();
        roots.Visit(source.Expression);
        var entity = roots.Model?.FindEntityType(entityType);
        if (entity is null)
        {
            // Multiple ownerships can share one CLR type; this navigation already identifies its actual ownership.
            var candidates = roots.Model?.GetEntityTypes().Where(e => e.ClrType == entityType).ToArray();
            return candidates is { Length: > 0 } && candidates.All(e => e.IsOwned()) ? source : null;
        }
        // Owned items have one owner; the owner's source is normalized before its navigation is expanded.
        if (entity.IsOwned()) return source;
        return provider.CreateQuery(new EntityQueryRootExpression(provider, entity));
    }

    private sealed class FindModel : ExpressionVisitor
    {
        internal IModel? Model { get; private set; }
        protected override Expression VisitExtension(Expression node)
        {
            if (node is EntityQueryRootExpression entity) Model ??= entity.EntityType.Model;
            return node is QueryRootExpression ? node : base.VisitExtension(node);
        }
    }
}
