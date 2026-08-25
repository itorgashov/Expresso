using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class IndexOfFunc : AbstractFunction
    {
        public IndexOfFunc(AbstractExpression sourceString, AbstractExpression find)
        {
            AssertNotNull(sourceString, nameof(sourceString));
            AssertExpressionOfTypes(sourceString, nameof(sourceString), typeof(string));
            AssertNotNull(find, nameof(find));
            AssertExpressionOfTypes(find, nameof(find), typeof(string));

            Arguments.Add(sourceString);
            Arguments.Add(find);
            ReturnType = typeof(int);
        }
    }
}
