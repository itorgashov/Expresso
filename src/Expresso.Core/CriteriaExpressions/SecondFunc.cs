using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class SecondFunc : DateTimeSingleArgIntFunction
    {
        public SecondFunc(AbstractExpression argument) : base(argument, DateTimeTypes.Time)
        {
        }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            visitor.VisitSecond(this, context);
    }
}
