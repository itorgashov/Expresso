namespace Expresso.Core.CriteriaExpressions.Abstract
{
    public abstract class AbstractExpression
    {
        public Type ReturnType { get; protected set; }

        protected void AssertNotNull(object o, string argumentName)
        {
            if (o is null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        protected void AssertObjectOfTypes(object o, string argumentName, params Type[] types)
        {
            foreach (Type type in types)
            {
                if (type.IsInstanceOfType(o)) return;
            }
            throw new ArgumentException($"Argument {argumentName} of type {o.GetType().Name} is not one of the expected types: {string.Join(", ", types.Select(t => t.Name))}");
        }

        protected void AssertExpressionOfTypes(AbstractExpression expression, string argumentName, params Type[] types)
        {
            foreach (Type type in types)
            {
                if (expression.ReturnType == type) return;
            }
            throw new ArgumentException($"Argument {argumentName} of type {expression.ReturnType.Name} is not one of the expected types: {string.Join(", ", types.Select(t => t.Name))}");
        }

        protected void AssertArgumentCollectionCountNotLess(IReadOnlyList<AbstractExpression> arguments, string argumentName, int minimalCount)
        {
            if (arguments.Count < minimalCount)
            {
                throw new ArgumentException($"List of argument {argumentName} contains less elements than expected: {minimalCount}");
            }
        }

        protected void AssertEachNotNull(IReadOnlyList<AbstractExpression> arguments, string argumentName)
        {
            foreach (var argument in arguments)
            {
                if (argument is null)
                {
                    throw new ArgumentException("Null expression is not allowed in the argument list", nameof(argument));
                }
            }
        }

        protected void AssertEachExpressionOfTypes(IReadOnlyList<AbstractExpression> arguments, string argumentName, params Type[] validTypes)
        {
            foreach (var argument in arguments)
            {
                bool match = true;
                Type invalidType = null!;
                foreach (Type type in validTypes)
                {
                    if (argument.ReturnType != type)
                    {
                        match = false;
                        invalidType = type;
                        break;
                    }
                }
                if (!match)
                {
                    throw new ArgumentException($"One or more expression in {argumentName} of type {invalidType.Name} which is not one of the expected types: {string.Join(", ", validTypes.Select(t => t.Name))}");
                }
            }
        }
        public override bool Equals(object? obj)
        {
            if (obj is null || GetType() != obj.GetType())
                return false;

            var other = (AbstractExpression)obj;
            return ReturnType == other.ReturnType;
        }

        public override int GetHashCode()
        {
            return ReturnType.GetHashCode();
        }

        public static bool operator ==(AbstractExpression left, AbstractExpression right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left is null || right is null)
                return false;
            return left.Equals(right);
        }

        public static bool operator !=(AbstractExpression left, AbstractExpression right)
        {
            return !(left == right);
        }
    }
}
