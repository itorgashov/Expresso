namespace Expresso.Core.CriteriaExpressions.Abstract
{
    public abstract class NumericArithFunction : AbstractFunction
    {
        protected NumericArithFunction(AbstractExpression argument1, AbstractExpression argument2)
        {
            AssertNotNull(argument1, nameof(argument1));
            AssertNotNull(argument2, nameof(argument2));

            bool isArg1Number = argument1.ReturnType == typeof(byte) 
                || argument1.ReturnType == typeof(int) 
                || argument1.ReturnType == typeof(double);
            bool isArg2Number = argument2.ReturnType == typeof(byte) 
                || argument2.ReturnType == typeof(int) 
                || argument2.ReturnType == typeof(double);
            if (!isArg1Number)
            {
                throw new ArgumentException("Illegal argument type", nameof(argument1));
            }
            if (!isArg2Number)
            {
                throw new ArgumentException("Illegal argument type", nameof(argument2));
            }

            Arguments.Add(argument1);
            Arguments.Add(argument2);
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
