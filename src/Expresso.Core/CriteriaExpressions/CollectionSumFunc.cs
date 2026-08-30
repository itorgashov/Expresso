using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class CollectionSumFunc : CollectionAggregateFunction
    {
        public CollectionSumFunc(CollectionRef collection, AbstractExpression selector)
            : base(collection, selector)
        {
            if (!IsNumeric(selector.ReturnType))
            {
                throw new ArgumentException("Illegal argument type", nameof(selector));
            }

            ReturnType = selector.ReturnType == typeof(double) ? typeof(double) : typeof(int);
        }
    }
}
