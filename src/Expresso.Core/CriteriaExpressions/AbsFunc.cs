using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class AbsFunc : NumericSingleArgFunction
    {
        public AbsFunc(AbstractExpression argument) : base(argument)
        {
            ReturnType = argument.ReturnType;
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
