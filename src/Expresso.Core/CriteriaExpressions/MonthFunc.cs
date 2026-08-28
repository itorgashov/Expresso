using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class MonthFunc : DateTimeSingleArgIntFunction
    {
        public MonthFunc(AbstractExpression argument) : base(argument, DateTimeTypes.Calendar)
        {
        }
    }
}
