using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class CeilingFunc : NumericSingleArgDoubleFunction
    {
        public CeilingFunc(AbstractExpression argument) : base(argument)
        {
        }
    }
}
