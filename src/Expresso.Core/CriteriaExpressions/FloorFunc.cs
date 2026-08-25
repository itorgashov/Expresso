using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class FloorFunc : NumericSingleArgDoubleFunction
    {
        public FloorFunc(AbstractExpression argument) : base(argument)
        {
        }
    }
}
