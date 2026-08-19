using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class NotFunc : BooleanFunction
    {
        public NotFunc(AbstractExpression argument) : base()
        {
            AssertNotNull(argument, nameof(argument));
            AssertExpressionOfTypes(argument, nameof(argument), typeof(bool));

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
