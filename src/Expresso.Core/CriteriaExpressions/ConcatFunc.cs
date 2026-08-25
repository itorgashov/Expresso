using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class ConcatFunc : StringFunction
    {
        public ConcatFunc(IReadOnlyList<AbstractExpression> arguments)
        {
            AssertNotNull(arguments, nameof(arguments));
            AssertArgumentCollectionCountNotLess(arguments, nameof(arguments), 2);
            AssertEachNotNull(arguments, nameof(arguments));
            AssertEachExpressionOfTypes(arguments, nameof(arguments), typeof(string));

            Arguments.AddRange(arguments);
        }
    }
}
