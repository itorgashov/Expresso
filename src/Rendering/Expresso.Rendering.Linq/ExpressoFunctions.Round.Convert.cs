using System.Numerics;

namespace Expresso.Rendering.Linq
{
    public static partial class ExpressoFunctions
    {
        /// <summary>Nearest finite double to <paramref name="magnitude"/> × 10^<paramref name="exponent"/>, ties to even.</summary>
        /// <exception cref="NotSupportedException">The value is outside the finite <see cref="double"/> range.</exception>
        private static double ToFiniteDouble(decimal magnitude, int exponent, bool negative)
        {
            var bits = decimal.GetBits(magnitude);
            var coefficient = new BigInteger((uint)bits[0])
                + (new BigInteger((uint)bits[1]) << 32)
                + (new BigInteger((uint)bits[2]) << 64);
            var scale = (bits[3] >> 16) & 0xFF;
            var tenExp = exponent - scale;
            var exp2 = (int)Math.Floor(Math.Log((double)magnitude, 2d) + exponent * Math.Log(10d, 2d));
            while (CompareToPow2(coefficient, tenExp, exp2) < 0)
            {
                exp2--;
            }

            while (CompareToPow2(coefficient, tenExp, exp2 + 1) >= 0)
            {
                exp2++;
            }

            ulong magnitudeBits;
            if (exp2 > 1023)
            {
                throw new NotSupportedException("value out of range: overflow");
            }

            if (exp2 < -1022)
            {
                var sub = RoundToEven(coefficient, tenExp, -1074);
                magnitudeBits = sub >= 1L << 52 ? 1UL << 52 : (ulong)sub;
            }
            else
            {
                var significand = RoundToEven(coefficient, tenExp, exp2 - 52);
                if (significand >= 1L << 53)
                {
                    exp2++;
                    significand = 1L << 52;
                }

                if (exp2 > 1023)
                {
                    throw new NotSupportedException("value out of range: overflow");
                }

                magnitudeBits = ((ulong)(exp2 + 1023) << 52) | ((ulong)significand - (1UL << 52));
            }

            if (negative && magnitudeBits != 0)
            {
                magnitudeBits |= 1UL << 63;
            }

            return BitConverter.ToDouble(BitConverter.GetBytes(magnitudeBits), 0);
        }

        private static int CompareToPow2(BigInteger coefficient, int tenExp, int exp2)
        {
            BigInteger left;
            BigInteger right;
            if (tenExp >= 0)
            {
                left = coefficient * BigInteger.Pow(5, tenExp);
                var two = tenExp - exp2;
                if (two >= 0)
                {
                    left <<= two;
                    right = BigInteger.One;
                }
                else
                {
                    right = BigInteger.One << -two;
                }
            }
            else
            {
                right = BigInteger.Pow(5, -tenExp);
                left = coefficient;
                var two = -tenExp + exp2;
                if (two >= 0)
                {
                    right <<= two;
                }
                else
                {
                    left <<= -two;
                }
            }

            return left.CompareTo(right);
        }

        private static long RoundToEven(BigInteger coefficient, int tenExp, int twoShift)
        {
            var numerator = coefficient;
            var denominator = BigInteger.One;
            if (tenExp >= 0)
            {
                numerator *= BigInteger.Pow(5, tenExp);
                var two = tenExp - twoShift;
                if (two >= 0)
                {
                    numerator <<= two;
                }
                else
                {
                    denominator <<= -two;
                }
            }
            else
            {
                denominator *= BigInteger.Pow(5, -tenExp);
                var two = -tenExp + twoShift;
                if (two >= 0)
                {
                    denominator <<= two;
                }
                else
                {
                    numerator <<= -two;
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
    }
}
