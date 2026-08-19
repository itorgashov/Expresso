using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class IsNullFunc : BooleanFunction
    {
        public IsNullFunc(AbstractExpression argument) : base()
        {
            AssertNotNull(argument, nameof(argument));
            AssertExpressionOfTypes(argument, nameof(argument), typeof(bool), typeof(string), typeof(byte), typeof(int), typeof(double), typeof(DateTime));

            Arguments.Add(argument);
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
