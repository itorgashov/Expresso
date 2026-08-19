using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class DivFunc : NumericArithFunction
    {
        public DivFunc(AbstractExpression argument1, AbstractExpression argument2) : base(argument1, argument2)
        {
            ReturnType = argument1.ReturnType;
        }

        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
