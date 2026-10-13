using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.EntityFramework;

public partial class Ef6ExpressionToLinqTransformer : IProjectionEntitySourceProvider
{
    /// <inheritdoc />
    IQueryable? IProjectionEntitySourceProvider.GetEntitySource(IQueryable source, Type entityType) =>
        Ef6ProjectionEntitySource.Resolve(source, entityType);
}

internal static class Ef6ProjectionEntitySource
{
    private static readonly MethodInfo CreateSet = typeof(ObjectContext).GetMethods()
        .Single(m => m.Name == nameof(ObjectContext.CreateObjectSet) && m.GetParameters().Length == 0);

    internal static IQueryable? Resolve(IQueryable source, Type entityType)
    {
        var roots = new FindContext();
        roots.Visit(source.Expression);
        if (roots.Context is null || entityType.IsValueType) return null;
        var workspace = roots.Context.MetadataWorkspace;
        var objectItems = (ObjectItemCollection)workspace.GetItemCollection(DataSpace.OSpace);
        var mappedType = workspace.GetItems<EntityType>(DataSpace.OSpace)
            .SingleOrDefault(type => objectItems.GetClrType(type) == entityType);
        // Entity sets belong to the metadata root, not a CLR-derived type (TPH and TPT use the same path).
        while (mappedType?.BaseType is EntityType parent) mappedType = parent;
        var setType = mappedType is null ? entityType : objectItems.GetClrType(mappedType);
        try
        {
            var query = (ObjectQuery)CreateSet.MakeGenericMethod(setType).Invoke(roots.Context, null)!;
            query.MergeOption = MergeOption.NoTracking;
            var expression = ((IQueryable)query).Expression;
            if (setType != entityType)
                expression = Expression.Call(typeof(Queryable), nameof(Queryable.OfType), new[] { entityType }, expression);
            return source.Provider.CreateQuery(expression);
        }
        catch (TargetInvocationException exception)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException!).Throw();
            throw;
        }
    }

    private sealed class FindContext : ExpressionVisitor
    {
        internal ObjectContext? Context { get; private set; }
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is ObjectQuery query) Context ??= query.Context;
            return node;
        }
    }
}
