using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class PowerFunc : NumericArithFunction
    {
        public PowerFunc(AbstractExpression argument1, AbstractExpression argument2) : base(argument1, argument2)
        {
            ReturnType = typeof(double);
        }
    }
}
