using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class LowerFunc : StringSingleArgFunction
    {
        public LowerFunc(AbstractExpression argument) : base(argument)
        {
        }
    }
}
