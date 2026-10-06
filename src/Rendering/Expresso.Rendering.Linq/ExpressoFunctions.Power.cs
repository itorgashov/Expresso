namespace Expresso.Rendering.Linq
{
    public static partial class ExpressoFunctions
    {
        /// <summary>
        /// PostgreSQL <c>power</c>: rejects a negative base with a fractional exponent, zero to a negative exponent,
        /// and a finite input whose result overflows or underflows.
        /// </summary>
        /// <exception cref="NotSupportedException">The arguments are outside the PostgreSQL domain or range.</exception>
        public static double Power(double baseValue, double exponent)
        {
            if (double.IsNaN(baseValue))
            {
                return double.IsNaN(exponent) || exponent != 0.0 ? double.NaN : 1.0;
            }

            if (double.IsNaN(exponent))
            {
                return baseValue != 1.0 ? double.NaN : 1.0;
            }

            if (baseValue == 0.0 && exponent < 0.0)
            {
                throw new NotSupportedException("zero raised to a negative power is undefined");
            }

            if (baseValue < 0.0 && Math.Floor(exponent) != exponent)
            {
                throw new NotSupportedException("a negative number raised to a non-integer power yields a complex result");
            }

            if (double.IsInfinity(exponent))
            {
                var magnitude = Math.Abs(baseValue);
                if (magnitude == 1.0)
                {
                    return 1.0;
                }

                return exponent > 0.0
                    ? magnitude > 1.0 ? exponent : 0.0
                    : magnitude > 1.0 ? 0.0 : -exponent;
            }

            if (double.IsInfinity(baseValue))
            {
                if (exponent == 0.0)
                {
                    return 1.0;
                }

                if (baseValue > 0.0)
                {
                    return exponent > 0.0 ? baseValue : 0.0;
                }

                var odd = Math.Floor(exponent / 2.0) != exponent / 2.0;
                if (exponent > 0.0)
                {
                    return odd ? baseValue : -baseValue;
                }

                return odd ? -0.0 : 0.0;
            }

            var result = Math.Pow(baseValue, exponent);
            if (double.IsNaN(result))
            {
                var magnitude = Math.Abs(baseValue);
                if (baseValue == 0.0)
                {
                    return 0.0;
                }

                if (magnitude == 1.0)
                {
                    return 1.0;
                }

                var overflows = exponent >= 0.0 ? magnitude > 1.0 : magnitude < 1.0;
                throw new NotSupportedException(overflows ? Overflow : Underflow);
            }

            if (double.IsInfinity(result))
            {
                throw new NotSupportedException(Overflow);
            }

            if (baseValue != 0.0 && (result == 0.0 || RoundsToZero(baseValue, exponent, result)))
            {
                throw new NotSupportedException(Underflow);
            }

            return result;
        }

        /// <summary>
        /// <see cref="Math.Pow(double, double)"/> rounds <c>2^-1075</c> up to the minimum subnormal.
        /// PostgreSQL <c>pow</c> returns zero there, and <c>dpow</c> then reports underflow.
        /// </summary>
        private static bool RoundsToZero(double baseValue, double exponent, double result)
        {
            if (Math.Abs(result) > double.Epsilon)
            {
                return false;
            }

            var log2 = Math.Log(Math.Abs(baseValue)) / Math.Log(2.0);
            return exponent * log2 <= -1075.0;
        }

        private const string Overflow = "value out of range: overflow";
        private const string Underflow = "value out of range: underflow";
    }
}
