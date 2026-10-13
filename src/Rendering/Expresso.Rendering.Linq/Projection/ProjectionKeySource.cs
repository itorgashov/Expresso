using System.Linq.Expressions;

namespace Expresso.Rendering.Linq;

internal static class ProjectionKeySource
{
    // Retain authorization filters. Hosts supply an explicit authorized keySource to also avoid
    // re-evaluating mutable request filters; never silently discard an application's security scope.
    internal static IQueryable<T> Unwindow<T>(IQueryable<T> source)
    {
        var expression = new RemoveWindow().Visit(source.Expression)!;
        var queries = new HashSet<IQueryable>();
        for (Expression? current = expression; current is not null; current = OuterSource(current, queries))
            if (current is MethodCallExpression call && call.Method.DeclaringType == typeof(Queryable) &&
                call.Method.Name is "Skip" or "Take")
                throw new InvalidOperationException("Cannot infer an unwindowed child key source through this query operator. " +
                    "Supply an explicit authorized, unpaged keySource.");
        return source.Provider.CreateQuery<T>(expression);
    }

    // Follow only the outer source, never lambda bodies, join inputs, or other nested queries.
    private static Expression? OuterSource(Expression expression, HashSet<IQueryable> queries)
    {
        if (expression is UnaryExpression unary) return unary.Operand;
        if (expression is ConstantExpression { Value: IQueryable query })
        {
            if (!queries.Add(query)) return null;
            var wrapped = query.Expression;
            return wrapped is ConstantExpression constant && ReferenceEquals(constant.Value, query) ? null : wrapped;
        }
        if (expression is not MethodCallExpression call) return null;
        if (call.Object is not null && typeof(IQueryable).IsAssignableFrom(call.Object.Type)) return call.Object;
        return call.Arguments.Count > 0 && typeof(IQueryable).IsAssignableFrom(call.Arguments[0].Type)
            ? call.Arguments[0] : null;
    }

    private sealed class RemoveWindow : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable) &&
                node.Method.Name is "Skip" or "Take" or "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending")
                return Visit(node.Arguments[0]);
            // Only remove the outer request chain, not paging inside a join or subquery.
            if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == "Where")
                return node.Update(node.Object, new[] { Visit(node.Arguments[0]), node.Arguments[1] });
            if (node.Method.Name is "AsNoTracking" or "AsTracking" && node.Arguments.Count > 0 &&
                typeof(IQueryable).IsAssignableFrom(node.Arguments[0].Type))
                return node.Update(node.Object, new[] { Visit(node.Arguments[0]) }.Concat(node.Arguments.Skip(1)));
            if (node.Method.Name == "MergeAs" && node.Object is not null)
                return node.Update(Visit(node.Object), node.Arguments);
            return node;
        }
    }
}
