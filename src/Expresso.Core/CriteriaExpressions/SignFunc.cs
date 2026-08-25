using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class SignFunc : NumericSingleArgIntResultFunction
    {
        public SignFunc(AbstractExpression argument) : base(argument)
        {
        }
    }
}
