using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class LenFunc : AbstractFunction
    {
        public LenFunc(AbstractExpression argument)
        {
            AssertNotNull(argument, nameof(argument));
            AssertExpressionOfTypes(argument, nameof(argument), typeof(string));

            Arguments.Add(argument);
            ReturnType = typeof(int);
        }

        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            visitor.VisitLen(this, context);
    }
}
