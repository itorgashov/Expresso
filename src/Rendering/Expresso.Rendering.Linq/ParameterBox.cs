namespace Expresso.Rendering.Linq
{
    /// <summary>
    /// Holds one literal value. Lambdas read <see cref="Value"/> from a captured box, so LINQ providers
    /// bind it as a query parameter (the same way they bind a closure variable) instead of inlining it.
    /// </summary>
    /// <typeparam name="T">Literal type.</typeparam>
    public sealed class ParameterBox<T>
    {
        /// <summary>Literal value.</summary>
        public readonly T Value;

        /// <summary>Creates a box for <paramref name="value"/>.</summary>
        public ParameterBox(T value)
        {
            Value = value;
        }
    }
}
