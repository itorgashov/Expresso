using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class InFunc : BooleanFunction
    {
        public InFunc(IReadOnlyList<AbstractExpression> arguments)
        {
            AssertNotNull(arguments, nameof(arguments));
            AssertArgumentCollectionCountNotLess(arguments, nameof(arguments), 2);
            AssertEachNotNull(arguments, nameof(arguments));
            AssertEachExpressionOfTypes(arguments, nameof(arguments), arguments[0].ReturnType);

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
