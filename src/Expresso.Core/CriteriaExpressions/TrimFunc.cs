using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class TrimFunc : StringSingleArgFunction
    {
        public TrimFunc(AbstractExpression argument) : base(argument)
        {
        }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            visitor.VisitTrim(this, context);
    }
}
