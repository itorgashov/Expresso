using System.Linq.Expressions;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using V = Expresso.Core.CriteriaExpressions.IExpressoVisitor<Expresso.Rendering.Linq.LinqScope, Expresso.Rendering.Linq.LinqNode>;

namespace Expresso.Rendering.Linq
{
    public abstract partial class ExpressionToLinqTransformerBase
    {
        /// <summary>
        /// Collection <c>min</c>. <paramref name="selector"/> returns a nullable type; NULL items are ignored and
        /// an empty collection gives NULL. Default <c>Enumerable.Min&lt;TItem, TResult&gt;</c>.
        /// </summary>
        protected virtual Expression Min(Expression items, LambdaExpression selector) =>
            Expression.Call(LinqEx.EnumerableMethod(nameof(Enumerable.Min), 2, selector.Parameters[0].Type, selector.ReturnType), items, selector);

        /// <summary>Collection <c>max</c>; same NULL rules as <see cref="Min"/>. Default <c>Enumerable.Max&lt;TItem, TResult&gt;</c>.</summary>
        protected virtual Expression Max(Expression items, LambdaExpression selector) =>
            Expression.Call(LinqEx.EnumerableMethod(nameof(Enumerable.Max), 2, selector.Parameters[0].Type, selector.ReturnType), items, selector);

        /// <summary>Collection <c>sum</c> over an <c>int?</c> or <c>double?</c> selector. Default <c>Enumerable.Sum</c>.</summary>
        protected virtual Expression Sum(Expression items, LambdaExpression selector) =>
            Expression.Call(LinqEx.EnumerableSelectorMethod(nameof(Enumerable.Sum), selector.Parameters[0].Type, selector.ReturnType), items, selector);

        /// <summary>Collection <c>avg</c> over an <c>int?</c> or <c>double?</c> selector, returning <c>double?</c>. Default <c>Enumerable.Average</c>.</summary>
        protected virtual Expression Average(Expression items, LambdaExpression selector) =>
            Expression.Call(LinqEx.EnumerableSelectorMethod(nameof(Enumerable.Average), selector.Parameters[0].Type, selector.ReturnType), items, selector);

        /// <summary>Scalar node over a nullable-typed expression.</summary>
        protected static LinqNode FromNullable(Expression nullable)
        {
            var isNull = Expression.Equal(nullable, Expression.Constant(null, nullable.Type));
            var value = Nullable.GetUnderlyingType(nullable.Type) is null ? nullable : Expression.Property(nullable, "Value");
            return LinqNode.Scalar(value, isNull, nullable);
        }

        private (Expression Items, LinqScope Scope) Enter(CollectionRef collection, LinqScope s)
        {
            if (!s.Mapping.Collections.TryGetValue(collection.Name, out var mapping))
            {
                throw new ArgumentException($"No mapping for the {collection.Name} collection");
            }

            var items = LinqEx.Inline(mapping.Navigation, s.Item);
            var item = Expression.Parameter(mapping.Items.EntityType, collection.Name);
            return (items, new LinqScope(item, mapping.Items));
        }

        private static Expression AnyCall(Expression items, Expression? predicate, LinqScope scope) =>
            predicate is null
                ? Expression.Call(LinqEx.EnumerableMethod(nameof(Enumerable.Any), 1, scope.Item.Type), items)
                : Expression.Call(LinqEx.EnumerableMethod(nameof(Enumerable.Any), 2, scope.Item.Type), items, Expression.Lambda(predicate, scope.Item));

        private LinqNode Exists(CollectionQuantifierFunction node, LinqScope s, bool negate)
        {
            var (items, scope) = Enter(node.Collection, s);
            var any = AnyCall(items, node.Predicate is null ? null : Visit(node.Predicate, scope).WhenTrue, scope);
            return negate
                ? LinqNode.Boolean(LinqEx.Not(any), any, neverUnknown: true)
                : LinqNode.Boolean(any, LinqEx.Not(any), neverUnknown: true);
        }

        private LinqNode NullableAggregate(
            CollectionAggregateFunction node,
            LinqScope s,
            Func<Expression, LambdaExpression, Expression> aggregate,
            bool promote)
        {
            var (items, scope) = Enter(node.Collection, s);
            var selector = Visit(node.Selector, scope);
            if (promote)
            {
                selector = Promote(selector);
            }

            return FromNullable(aggregate(items, Expression.Lambda(selector.ToNullable(), scope.Item)));
        }

        LinqNode V.VisitAny(AnyFunc node, LinqScope s) => Exists(node, s, negate: false);

        LinqNode V.VisitNone(NoneFunc node, LinqScope s) => Exists(node, s, negate: true);

        LinqNode V.VisitAll(AllFunc node, LinqScope s)
        {
            if (node.Predicate is null)
            {
                return LinqNode.Boolean(LinqEx.True, LinqEx.False, neverUnknown: true);
            }

            var (items, scope) = Enter(node.Collection, s);
            var anyFalse = AnyCall(items, Visit(node.Predicate, scope).WhenFalse, scope);
            return LinqNode.Boolean(LinqEx.Not(anyFalse), anyFalse, neverUnknown: true);
        }

        LinqNode V.VisitCollectionCount(CollectionCountFunc node, LinqScope s)
        {
            var (items, scope) = Enter(node.Collection, s);
            var count = node.Predicate is null
                ? Expression.Call(LinqEx.EnumerableMethod(nameof(Enumerable.Count), 1, scope.Item.Type), items)
                : Expression.Call(LinqEx.EnumerableMethod(nameof(Enumerable.Count), 2, scope.Item.Type), items,
                    Expression.Lambda(Visit(node.Predicate, scope).WhenTrue, scope.Item));
            return LinqNode.Scalar(count, null);
        }

        LinqNode V.VisitCollectionMin(CollectionMinFunc node, LinqScope s) => NullableAggregate(node, s, Min, promote: false);

        LinqNode V.VisitCollectionMax(CollectionMaxFunc node, LinqScope s) => NullableAggregate(node, s, Max, promote: false);

        LinqNode V.VisitCollectionAvg(CollectionAvgFunc node, LinqScope s) => NullableAggregate(node, s, Average, promote: true);

        LinqNode V.VisitCollectionSum(CollectionSumFunc node, LinqScope s)
        {
            var (items, scope) = Enter(node.Collection, s);
            var selector = Promote(Visit(node.Selector, scope));
            var sum = Sum(items, Expression.Lambda(selector.ToNullable(), scope.Item));
            var hasValues = AnyCall(items, selector.NotNull, scope);
            return LinqNode.Scalar(Expression.Convert(sum, selector.Type), LinqEx.Not(hasValues));
        }
    }
}
