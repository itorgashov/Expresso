using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class HourFunc : DateTimeSingleArgIntFunction
    {
        public HourFunc(AbstractExpression argument) : base(argument)
        {
        }
    }
}
