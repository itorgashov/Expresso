using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class OrFunc : BooleanFunction
    {
        public OrFunc(IReadOnlyList<AbstractExpression> arguments) : base()
        {
            AssertNotNull(arguments, nameof(arguments));
            AssertArgumentCollectionCountNotLess(arguments, nameof(arguments), 2);
            AssertEachNotNull(arguments, nameof(arguments));
            AssertEachExpressionOfTypes(arguments, nameof(arguments), typeof(bool));

            Arguments.AddRange(arguments);
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
