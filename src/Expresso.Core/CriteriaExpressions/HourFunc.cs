using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class HourFunc : DateTimeSingleArgIntFunction
    {
        public HourFunc(AbstractExpression argument) : base(argument, DateTimeTypes.Time)
        {
        }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            visitor.VisitHour(this, context);
    }
}
