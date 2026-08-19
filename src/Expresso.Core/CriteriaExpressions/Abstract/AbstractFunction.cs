namespace Expresso.Core.CriteriaExpressions.Abstract
{
    public abstract class AbstractFunction : AbstractExpression
    {
        public List<AbstractExpression> Arguments { get; set; } = new();

        public override bool Equals(object? obj)
        {
            if (!base.Equals(obj))
                return false;

            var other = (AbstractFunction)obj;
            return Arguments.SequenceEqual(other.Arguments);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = base.GetHashCode();
                foreach (var arg in Arguments)
                {
                    hash = hash * 31 + (arg?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }
    }
}
