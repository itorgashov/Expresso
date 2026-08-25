using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class UpperFunc : StringSingleArgFunction
    {
        public UpperFunc(AbstractExpression argument) : base(argument)
        {
        }
    }
}
