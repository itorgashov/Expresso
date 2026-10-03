using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class AllFunc : CollectionQuantifierFunction
    {
        public AllFunc(CollectionRef collection, AbstractExpression? predicate = null)
            : base(collection, predicate)
        {
        }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            visitor.VisitAll(this, context);
    }
}
