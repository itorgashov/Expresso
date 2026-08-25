using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class LTrimFunc : StringSingleArgFunction
    {
        public LTrimFunc(AbstractExpression argument) : base(argument)
        {
        }
    }
}
