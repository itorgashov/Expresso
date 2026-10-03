using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class AddMinutesFunc : DateTimeAddFunction
    {
        public AddMinutesFunc(AbstractExpression dateTime, AbstractExpression amount) : base(dateTime, amount, DateTimeTypes.Time)
        {
        }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            visitor.VisitAddMinutes(this, context);
    }
}
