using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class CollectionAvgFunc : CollectionAggregateFunction
    {
        public CollectionAvgFunc(CollectionRef collection, AbstractExpression selector)
            : base(collection, selector)
        {
            if (!IsNumeric(selector.ReturnType))
            {
                throw new ArgumentException("Illegal argument type", nameof(selector));
            }

            ReturnType = typeof(double);
        }
    }
}
