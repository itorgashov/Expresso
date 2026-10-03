using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class SignFunc : NumericSingleArgIntResultFunction
    {
        public SignFunc(AbstractExpression argument) : base(argument)
        {
        }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            visitor.VisitSign(this, context);
    }
}
