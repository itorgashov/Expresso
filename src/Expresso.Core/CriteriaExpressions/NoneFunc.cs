using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class NoneFunc : CollectionQuantifierFunction
    {
        public NoneFunc(CollectionRef collection, AbstractExpression? predicate = null)
            : base(collection, predicate)
        {
        }
    }
}
