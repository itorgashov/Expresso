namespace Expresso.Core.CriteriaExpressions.Abstract
{
    public abstract class NumericSingleArgFunction : AbstractFunction
    {
        protected NumericSingleArgFunction(AbstractExpression argument)
        {
            AssertNotNull(argument, nameof(argument));

            bool isNumber = argument.ReturnType == typeof(byte) 
                || argument.ReturnType == typeof(int) 
                || argument.ReturnType == typeof(double);
            if (!isNumber)
            {
                throw new ArgumentException("Illegal argument type", nameof(argument));
            }

            Arguments.Add(argument);
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
