using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class DayFunc : DateTimeSingleArgIntFunction
    {
        public DayFunc(AbstractExpression argument) : base(argument)
        {
        }
    }
}
