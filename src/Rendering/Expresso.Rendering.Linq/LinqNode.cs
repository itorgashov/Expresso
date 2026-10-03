using System.Linq.Expressions;

namespace Expresso.Rendering.Linq
{
    /// <summary>
    /// Rendered form of one IR node.
    /// <para>
    /// A scalar carries <see cref="Value"/> (the non-null value, of the underlying type) and
    /// <see cref="IsNull"/> (<see langword="null"/> when the value can never be null).
    /// <see cref="Value"/> is only evaluated where <see cref="IsNull"/> is false.
    /// </para>
    /// <para>
    /// A boolean carries SQL three-valued logic as two plain conditions: <see cref="WhenTrue"/> and
    /// <see cref="WhenFalse"/>. When neither holds the result is unknown (SQL NULL).
    /// </para>
    /// </summary>
    public sealed class LinqNode
    {
        private readonly Expression? _whenTrue;
        private readonly Expression? _whenFalse;
        private readonly Expression? _nullable;

        private LinqNode(Expression value, Expression? isNull, Expression? nullable, Expression? whenTrue, Expression? whenFalse)
        {
            Value = value;
            IsNull = isNull;
            _nullable = nullable;
            _whenTrue = whenTrue;
            _whenFalse = whenFalse;
        }

        /// <summary>Non-null value. For a boolean node this is <see cref="WhenTrue"/>.</summary>
        public Expression Value { get; }

        /// <summary>Condition that the value is NULL, or <see langword="null"/> when it never is.</summary>
        public Expression? IsNull { get; }

        /// <summary>True for boolean function results (three-valued).</summary>
        public bool IsBoolean => _whenTrue is not null;

        /// <summary>Type of <see cref="Value"/>.</summary>
        public Type Type => Value.Type;

        /// <summary>Condition that the boolean is TRUE.</summary>
        public Expression WhenTrue => _whenTrue ?? LinqEx.AndAlso(NotNull, RequireBool(Value));

        /// <summary>Condition that the boolean is FALSE.</summary>
        public Expression WhenFalse => _whenFalse ?? LinqEx.AndAlso(NotNull, LinqEx.Not(RequireBool(Value)));

        /// <summary>Condition that the value is not NULL, or <see langword="null"/> when it never is.</summary>
        public Expression? NotNull => IsNull is null ? null : LinqEx.Not(IsNull);

        /// <summary>Creates a scalar node.</summary>
        /// <param name="value">Non-null value.</param>
        /// <param name="isNull">NULL condition, or <see langword="null"/> when never null.</param>
        /// <param name="nullable">Optional ready-made nullable form (for example the mapped member itself).</param>
        public static LinqNode Scalar(Expression value, Expression? isNull, Expression? nullable = null) =>
            new(value ?? throw new ArgumentNullException(nameof(value)), isNull, nullable, null, null);

        /// <summary>Creates a boolean node.</summary>
        /// <param name="whenTrue">Condition for TRUE.</param>
        /// <param name="whenFalse">Condition for FALSE.</param>
        /// <param name="neverUnknown">True when one of the two conditions always holds.</param>
        public static LinqNode Boolean(Expression whenTrue, Expression whenFalse, bool neverUnknown)
        {
            var isNull = neverUnknown ? null : LinqEx.Not(LinqEx.OrElse(whenTrue, whenFalse));
            return new LinqNode(whenTrue, isNull, null, whenTrue, whenFalse);
        }

        /// <summary>Value as a nullable-typed expression (NULL when <see cref="IsNull"/> holds).</summary>
        public Expression ToNullable()
        {
            if (_nullable is not null)
            {
                return _nullable;
            }

            var nullableType = LinqEx.NullableType(Type);
            var value = nullableType == Type ? Value : Expression.Convert(Value, nullableType);
            return IsNull is null
                ? value
                : Expression.Condition(IsNull, Expression.Constant(null, nullableType), value);
        }

        private static Expression RequireBool(Expression value) =>
            value.Type == typeof(bool)
                ? value
                : throw new InvalidOperationException($"Expression of type {value.Type.Name} is not a boolean.");
    }
}
