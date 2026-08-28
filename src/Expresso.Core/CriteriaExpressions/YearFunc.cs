using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class YearFunc : DateTimeSingleArgIntFunction
    {
        public YearFunc(AbstractExpression argument) : base(argument, DateTimeTypes.Calendar)
        {
        }
    }
}
