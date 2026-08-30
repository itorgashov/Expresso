using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class CollectionMaxFunc : CollectionAggregateFunction
    {
        public CollectionMaxFunc(CollectionRef collection, AbstractExpression selector)
            : base(collection, selector)
        {
            if (!IsMinMaxSelectorType(selector.ReturnType))
            {
                throw new ArgumentException("Illegal argument type", nameof(selector));
            }

            ReturnType = selector.ReturnType;
        }
    }
}
