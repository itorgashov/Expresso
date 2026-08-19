namespace Expresso.Core.CriteriaExpressions.Abstract
{
    public abstract class BooleanFunction : AbstractFunction
    {
        public BooleanFunction()
        {
            ReturnType = typeof(bool);
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
