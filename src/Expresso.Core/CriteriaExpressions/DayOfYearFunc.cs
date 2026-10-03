using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class DayOfYearFunc : DateTimeSingleArgIntFunction
    {
        public DayOfYearFunc(AbstractExpression argument) : base(argument, DateTimeTypes.Calendar)
        {
        }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            visitor.VisitDayOfYear(this, context);
    }
}
