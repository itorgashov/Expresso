using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Rendering.TestCases
{
    public sealed class DummyBooleanFunction : BooleanFunction
    {
        public override TResult Accept<TContext, TResult>(IExpressoVisitor<TContext, TResult> visitor, TContext context) =>
            throw new NotSupportedException($"Expression type '{nameof(DummyBooleanFunction)}' is not supported.");
    }
}
