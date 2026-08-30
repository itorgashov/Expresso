using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class CollectionRef : AbstractExpression
    {
        public CollectionRef(string name, string? scope = null)
        {
            AssertNotNull(name, nameof(name));
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Collection name must not be empty.", nameof(name));
            }

            Name = name.ToLowerInvariant();
            Scope = scope;
            ReturnType = typeof(CollectionRef);
        }

        public string Name { get; }

        public string? Scope { get; }

        public override bool Equals(object? obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            var other = (CollectionRef)obj;
            return Name == other.Name && Scope == other.Scope;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = base.GetHashCode();
                hash = hash * 31 + Name.GetHashCode();
                hash = hash * 31 + (Scope?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
