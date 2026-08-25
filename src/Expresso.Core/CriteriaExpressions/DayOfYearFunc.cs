using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class DayOfYearFunc : DateTimeSingleArgIntFunction
    {
        public DayOfYearFunc(AbstractExpression argument) : base(argument)
        {
        }
    }
}
