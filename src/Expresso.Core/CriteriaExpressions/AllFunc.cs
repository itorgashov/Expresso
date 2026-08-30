using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class AllFunc : CollectionQuantifierFunction
    {
        public AllFunc(CollectionRef collection, AbstractExpression? predicate = null)
            : base(collection, predicate)
        {
        }
    }
}
