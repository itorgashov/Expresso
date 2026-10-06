using System.Numerics;

namespace Expresso.Rendering.Linq
{
    public static partial class ExpressoFunctions
    {
        private const long Ten14 = 100000000000000L;
        private const long Ten15 = 1000000000000000L;

        /// <summary>
        /// Keeps 15 significant digits with ties to even, then rounds half away from zero at <paramref name="digits"/> decimal places.
        /// </summary>
        /// <exception cref="NotSupportedException">The rounded value is outside the finite <see cref="double"/> range.</exception>
        public static double Round(double value, int digits)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value == 0d)
            {
                return value;
            }

            // A direct decimal-to-double cast can land on the neighbor of the 15-digit text form.
            if (Math.Abs(value) < 7.9e27 && digits < -28)
            {
                return 0d;
            }

            return RoundScaled(value, digits);
        }

        /// <summary>
        /// Rounds the exact binary value to 15 significant digits (ties to even), then applies the requested precision.
        /// The exponent is that decimal magnitude, so a subnormal never divides by a zero power of ten, and
        /// <paramref name="digits"/> is added in a <c>long</c> so <see cref="int.MaxValue"/> does not wrap.
        /// </summary>
        private static double RoundScaled(double value, int digits)
        {
            var negative = value < 0d;
            FifteenDigits(Math.Abs(value), out var significand, out var exponent);
            var places = (long)digits + exponent;
            if (places < -28)
            {
                return 0d;
            }

            decimal rounded;
            if (places >= 14)
            {
                rounded = significand;
            }
            else if (places >= 0)
            {
                rounded = Math.Round(significand, (int)places, MidpointRounding.AwayFromZero);
            }
            else
            {
                var factor = Pow10((int)-places);
                rounded = Math.Round(significand / factor, MidpointRounding.AwayFromZero) * factor;
            }
            if (rounded == 0m)
            {
                return 0d;
            }

            if (rounded >= 10m)
            {
                rounded /= 10m;
                exponent++;
            }

            return ToFiniteDouble(rounded, exponent, negative);
        }

        /// <summary>15-digit significand in [1, 10) and its power of ten. Ties in that conversion resolve to even.</summary>
        private static void FifteenDigits(double absolute, out decimal significand, out int exponent)
        {
            var bits = BitConverter.ToUInt64(BitConverter.GetBytes(absolute), 0);
            var rawExponent = (int)((bits >> 52) & 0x7FF);
            var fraction = bits & 0xFFFFFFFFFFFFFUL;
            BigInteger mantissa;
            int power;
            if (rawExponent == 0)
            {
                mantissa = fraction;
                power = -1074;
            }
            else
            {
                mantissa = fraction | (1UL << 52);
                power = rawExponent - 1075;
            }

            var exp10 = (int)Math.Floor(Math.Log10(absolute));
            while (ComparePow10(mantissa, power, exp10) < 0)
            {
                exp10--;
            }

            while (ComparePow10(mantissa, power, exp10 + 1) >= 0)
            {
                exp10++;
            }

            var digits = RoundTiesToEven(mantissa, power, exp10 - 14);
            if (digits >= Ten15)
            {
                significand = 1m;
                exponent = exp10 + 1;
                return;
            }

            significand = digits / (decimal)Ten14;
            exponent = exp10;
        }

        private static int ComparePow10(BigInteger mantissa, int power, int exp10)
        {
            BigInteger left;
            BigInteger right;
            if (exp10 >= 0)
            {
                var ten = BigInteger.Pow(10, exp10);
                if (power >= 0)
                {
                    left = mantissa << power;
                    right = ten;
                }
                else
                {
                    left = mantissa;
                    right = ten << -power;
                }
            }
            else
            {
                var ten = BigInteger.Pow(10, -exp10);
                if (power >= 0)
                {
                    left = (mantissa << power) * ten;
                    right = BigInteger.One;
                }
                else
                {
                    left = mantissa * ten;
                    right = BigInteger.One << -power;
                }
            }

            return left.CompareTo(right);
        }

        private static long RoundTiesToEven(BigInteger mantissa, int power, int tenExp)
        {
            BigInteger numerator;
            BigInteger denominator;
            if (tenExp >= 0)
            {
                var fives = BigInteger.Pow(5, tenExp);
                var twoExp = power - tenExp;
                if (twoExp >= 0)
                {
                    numerator = mantissa << twoExp;
                    denominator = fives;
                }
                else
                {
                    numerator = mantissa;
                    denominator = fives << -twoExp;
                }
            }
            else
            {
                var scale = -tenExp;
                var fives = BigInteger.Pow(5, scale);
                numerator = mantissa * fives;
                var twoExp = power + scale;
                if (twoExp >= 0)
                {
                    numerator <<= twoExp;
                    denominator = BigInteger.One;
                }
                else
                {
                    denominator = BigInteger.One << -twoExp;
                }
            }

            var quotient = BigInteger.DivRem(numerator, denominator, out var remainder);
            var twice = remainder << 1;
            if (twice > denominator || (twice == denominator && (quotient & 1) != 0))
            {
                quotient += 1;
            }

            return (long)quotient;
        }

        private static decimal Pow10(int exponent)
        {
            var result = 1m;
            for (var i = 0; i < exponent; i++)
            {
                result *= 10m;
            }

            return result;
        }
    }
}
