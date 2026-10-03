using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class SqrtFunc : NumericSingleArgDoubleFunction
    {
        public SqrtFunc(AbstractExpression argument) : base(argument)
        {
        }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            visitor.VisitSqrt(this, context);
    }
}
