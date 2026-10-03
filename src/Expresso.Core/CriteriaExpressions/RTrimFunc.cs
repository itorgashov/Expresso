using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class RTrimFunc : StringSingleArgFunction
    {
        public RTrimFunc(AbstractExpression argument) : base(argument)
        {
        }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            visitor.VisitRTrim(this, context);
    }
}
