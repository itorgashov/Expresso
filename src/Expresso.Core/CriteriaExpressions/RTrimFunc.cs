using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class RTrimFunc : StringSingleArgFunction
    {
        public RTrimFunc(AbstractExpression argument) : base(argument)
        {
        }
    }
}
