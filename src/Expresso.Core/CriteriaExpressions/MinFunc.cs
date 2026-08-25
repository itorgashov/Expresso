using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class MinFunc : NumericArithFunction
    {
        public MinFunc(AbstractExpression argument1, AbstractExpression argument2) : base(argument1, argument2)
        {
            ReturnType = argument1.ReturnType;
        }
    }
}
