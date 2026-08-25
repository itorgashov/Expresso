namespace Expresso.Core.CriteriaExpressions.Abstract
{
    public abstract class NumericSingleArgDoubleFunction : NumericSingleArgFunction
    {
        protected NumericSingleArgDoubleFunction(AbstractExpression argument) : base(argument)
        {
            ReturnType = typeof(double);
        }
    }
}
