using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class SqrtFunc : NumericSingleArgDoubleFunction
    {
        public SqrtFunc(AbstractExpression argument) : base(argument)
        {
        }
    }
}
