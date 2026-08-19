namespace Expresso.Core.CriteriaExpressions.Abstract
{
    public abstract class StringFunction : AbstractFunction
    {
        public StringFunction()
        {
            ReturnType = typeof(string);
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
