using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class MinuteFunc : DateTimeSingleArgIntFunction
    {
        public MinuteFunc(AbstractExpression argument) : base(argument, DateTimeTypes.Time)
        {
        }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            visitor.VisitMinute(this, context);
    }
}
