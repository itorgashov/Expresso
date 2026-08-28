namespace Expresso.Core.CriteriaExpressions.Abstract
{
    public abstract class ComparisonFunction : BooleanFunction
    {
        protected ComparisonFunction(AbstractExpression leftOperand, AbstractExpression rightOperand)
        {
            AssertNotNull(leftOperand, nameof(leftOperand));
            AssertNotNull(rightOperand, nameof(rightOperand));

            if (!AreCompatibleTypes(leftOperand.ReturnType, rightOperand.ReturnType))
            {
                throw new ArgumentException("Incompatible argument types");
            }

            Arguments.Add(leftOperand);
            Arguments.Add(rightOperand);
        }

        private static bool AreCompatibleTypes(Type left, Type right)
        {
            if (left == right)
            {
                return true;
            }

            return IsNumeric(left) && IsNumeric(right);
        }

        private static bool IsNumeric(Type type) =>
            type == typeof(byte) || type == typeof(int) || type == typeof(double);

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
