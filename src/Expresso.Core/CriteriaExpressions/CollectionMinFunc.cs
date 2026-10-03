using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class CollectionMinFunc : CollectionAggregateFunction
    {
        public CollectionMinFunc(CollectionRef collection, AbstractExpression selector)
            : base(collection, selector)
        {
            if (!IsMinMaxSelectorType(selector.ReturnType))
            {
                throw new ArgumentException("Illegal argument type", nameof(selector));
            }

            ReturnType = selector.ReturnType;
        }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            visitor.VisitCollectionMin(this, context);
    }
}
