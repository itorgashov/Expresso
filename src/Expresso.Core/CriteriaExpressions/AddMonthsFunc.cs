using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class AddMonthsFunc : DateTimeAddFunction
    {
        public AddMonthsFunc(AbstractExpression dateTime, AbstractExpression amount) : base(dateTime, amount, DateTimeTypes.Calendar)
        {
        }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            visitor.VisitAddMonths(this, context);
    }
}
