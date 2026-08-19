using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class NeqFunc : ComparisonFunction
    {
        public NeqFunc(AbstractExpression leftOperand, AbstractExpression rightOperand) : base(leftOperand, rightOperand)
        {
            AssertNotNull(leftOperand, nameof(leftOperand));
            AssertNotNull(rightOperand, nameof(rightOperand));
            AssertExpressionOfTypes(leftOperand, nameof(leftOperand), typeof(byte), typeof(int), typeof(double), typeof(DateTime), typeof(bool), typeof(string));
            AssertExpressionOfTypes(rightOperand, nameof(rightOperand), typeof(byte), typeof(int), typeof(double), typeof(DateTime), typeof(bool), typeof(string));
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
