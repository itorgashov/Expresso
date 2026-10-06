namespace Expresso.Rendering.Linq
{
    public static partial class ExpressoFunctions
    {
        /// <summary>PostgreSQL <c>int4pl</c> / <c>float8pl</c>.</summary>
        /// <exception cref="NotSupportedException">The sum is outside the type's range.</exception>
        public static int Add(int left, int right) => CheckInt(() => checked(left + right));

        /// <inheritdoc cref="Add(int, int)"/>
        public static double Add(double left, double right) => CheckFloat(left + right, double.IsInfinity(left) || double.IsInfinity(right), zeroIsValid: true);

        /// <summary>PostgreSQL <c>int4mi</c> / <c>float8mi</c>.</summary>
        /// <exception cref="NotSupportedException">The difference is outside the type's range.</exception>
        public static int Subtract(int left, int right) => CheckInt(() => checked(left - right));

        /// <inheritdoc cref="Subtract(int, int)"/>
        public static double Subtract(double left, double right) => CheckFloat(left - right, double.IsInfinity(left) || double.IsInfinity(right), zeroIsValid: true);

        /// <summary>PostgreSQL <c>int4mul</c> / <c>float8mul</c>.</summary>
        /// <exception cref="NotSupportedException">The product is outside the type's range.</exception>
        public static int Multiply(int left, int right) => CheckInt(() => checked(left * right));

        /// <inheritdoc cref="Multiply(int, int)"/>
        public static double Multiply(double left, double right) =>
            CheckFloat(left * right, double.IsInfinity(left) || double.IsInfinity(right), left == 0.0 || right == 0.0);

        /// <summary>PostgreSQL <c>int4div</c>. <c>int.MinValue / -1</c> is a range error.</summary>
        /// <exception cref="NotSupportedException"><paramref name="right"/> is zero, or the quotient does not fit in <c>int</c>.</exception>
        public static int Divide(int left, int right)
        {
            if (right == 0)
            {
                throw new NotSupportedException("division by zero");
            }

            if (left == int.MinValue && right == -1)
            {
                throw new NotSupportedException("integer out of range");
            }

            return left / right;
        }

        /// <summary>PostgreSQL <c>int4mod</c>. A divisor of <c>-1</c> returns 0, including for <see cref="int.MinValue"/>.</summary>
        /// <exception cref="NotSupportedException"><paramref name="right"/> is zero.</exception>
        public static int Modulo(int left, int right)
        {
            if (right == 0)
            {
                throw new NotSupportedException("division by zero");
            }

            return right == -1 ? 0 : left % right;
        }

        private static int CheckInt(Func<int> compute)
        {
            try
            {
                return compute();
            }
            catch (OverflowException)
            {
                throw new NotSupportedException("integer out of range");
            }
        }

        private static double CheckFloat(double result, bool infinityIsValid, bool zeroIsValid)
        {
            if (double.IsInfinity(result) && !infinityIsValid)
            {
                throw new NotSupportedException("value out of range: overflow");
            }

            if (result == 0.0 && !zeroIsValid)
            {
                throw new NotSupportedException("value out of range: underflow");
            }

            return result;
        }
    }
}
