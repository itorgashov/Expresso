using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class DayFunc : DateTimeSingleArgIntFunction
    {
        public DayFunc(AbstractExpression argument) : base(argument, DateTimeTypes.Calendar)
        {
        }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            visitor.VisitDay(this, context);
    }
}
