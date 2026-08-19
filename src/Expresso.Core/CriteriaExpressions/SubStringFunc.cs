using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class SubStringFunc : StringFunction
    {
        public SubStringFunc(AbstractExpression sourceString, AbstractExpression startIndex, AbstractExpression length)
        {
            AssertNotNull(sourceString, nameof(sourceString));
            AssertExpressionOfTypes(sourceString, nameof(sourceString), typeof(string));
            AssertNotNull(startIndex, nameof(startIndex));
            AssertExpressionOfTypes(startIndex, nameof(startIndex), typeof(int));
            AssertNotNull(length, nameof(length));
            AssertExpressionOfTypes(length, nameof(length), typeof(int));

            Arguments.Add(sourceString);
            Arguments.Add(startIndex);
            Arguments.Add(length);
            ReturnType = typeof(string);
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
