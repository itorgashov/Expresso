using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class Field : AbstractExpression
    {
        public Field(string name, Type type)
        {
            AssertNotNull(name, nameof(name));
            AssertNotNull(type, nameof(type));

            Name = name;
            ReturnType = type;
        }
        public string Name
        {
            get;
            private set;
        }

        public override bool Equals(object? obj)
        {
            if (!base.Equals(obj))
                return false;

            var other = (Field)obj;
            return Name == other.Name;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = base.GetHashCode();
                hash = hash * 31 + (Name?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
