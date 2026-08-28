using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class LtFunc : ComparisonFunction
    {
        public LtFunc(AbstractExpression leftOperand, AbstractExpression rightOperand) : base(leftOperand, rightOperand)
        {
            AssertNotNull(leftOperand, nameof(leftOperand));
            AssertNotNull(rightOperand, nameof(rightOperand));
            AssertExpressionOfTypes(leftOperand, nameof(leftOperand), SupportedOperandTypes.Ordered);
            AssertExpressionOfTypes(rightOperand, nameof(rightOperand), SupportedOperandTypes.Ordered);
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
