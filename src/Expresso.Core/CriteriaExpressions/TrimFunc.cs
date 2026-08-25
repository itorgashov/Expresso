using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class TrimFunc : StringSingleArgFunction
    {
        public TrimFunc(AbstractExpression argument) : base(argument)
        {
        }
    }
}
