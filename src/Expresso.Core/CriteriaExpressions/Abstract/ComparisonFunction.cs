namespace Expresso.Core.CriteriaExpressions.Abstract
{
    public abstract class ComparisonFunction : BooleanFunction
    {
        protected ComparisonFunction(AbstractExpression leftOperand, AbstractExpression rightOperand)
        {
            AssertNotNull(leftOperand, nameof(leftOperand));
            AssertNotNull(rightOperand, nameof(rightOperand));

            bool isLeftNumber = leftOperand.ReturnType == typeof(byte) 
                || leftOperand.ReturnType == typeof(int) 
                || leftOperand.ReturnType == typeof(double);
            bool isRightNumber = rightOperand.ReturnType == typeof(byte) 
                || rightOperand.ReturnType == typeof(int) 
                || rightOperand.ReturnType == typeof(double);
            if (!(leftOperand.ReturnType == typeof(bool) && rightOperand.ReturnType == typeof(bool)
                || leftOperand.ReturnType == typeof(string) && rightOperand.ReturnType == typeof(string)
                || leftOperand.ReturnType == typeof(DateTime) && rightOperand.ReturnType == typeof(DateTime)
                || isLeftNumber && isRightNumber))
            {
                throw new ArgumentException("Incompatible argument types");
            }

            Arguments.Add(leftOperand);
            Arguments.Add(rightOperand);
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
