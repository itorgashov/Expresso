using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class DayOfWeekFunc : DateTimeSingleArgIntFunction
    {
        public DayOfWeekFunc(AbstractExpression argument) : base(argument, DateTimeTypes.Calendar)
        {
        }
    }
}
