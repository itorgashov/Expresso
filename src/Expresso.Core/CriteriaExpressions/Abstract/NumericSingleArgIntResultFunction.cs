namespace Expresso.Core.CriteriaExpressions.Abstract
{
    public abstract class NumericSingleArgIntResultFunction : NumericSingleArgFunction
    {
        protected NumericSingleArgIntResultFunction(AbstractExpression argument) : base(argument)
        {
            ReturnType = typeof(int);
        }
    }
}
