using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class AddMonthsFunc : DateTimeAddFunction
    {
        public AddMonthsFunc(AbstractExpression dateTime, AbstractExpression amount) : base(dateTime, amount, DateTimeTypes.Calendar)
        {
        }
    }
}
