using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class AddDaysFunc : DateTimeAddFunction
    {
        public AddDaysFunc(AbstractExpression dateTime, AbstractExpression amount) : base(dateTime, amount)
        {
        }
    }
}
