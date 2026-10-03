using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class LowerFunc : StringSingleArgFunction
    {
        public LowerFunc(AbstractExpression argument) : base(argument)
        {
        }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            visitor.VisitLower(this, context);
    }
}
