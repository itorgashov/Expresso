using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class MonthFunc : DateTimeSingleArgIntFunction
    {
        public MonthFunc(AbstractExpression argument) : base(argument, DateTimeTypes.Calendar)
        {
        }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            visitor.VisitMonth(this, context);
    }
}
