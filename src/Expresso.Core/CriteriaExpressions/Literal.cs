using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class Literal : AbstractExpression
    {
        public Literal(object @value)
        {
            AssertNotNull(@value, nameof(@value));

            Value = @value;
            ReturnType = Value.GetType();
        }

        public object Value
        {
            get;
            private set;
        }

        public override bool Equals(object? obj)
        {
            if (!base.Equals(obj))
                return false;

            var other = (Literal)obj;
            return Value.Equals(other.Value);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = base.GetHashCode();
                hash = hash * 31 + (Value?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
