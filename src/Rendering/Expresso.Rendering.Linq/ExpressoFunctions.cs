namespace Expresso.Rendering.Linq
{
    /// <summary>
    /// In-memory reference implementations with PostgreSQL semantics, used by <see cref="InMemoryExpressionToLinqTransformer"/>.
    /// Arguments are never NULL here: NULL propagation is handled by the transformer.
    /// </summary>
    public static partial class ExpressoFunctions
    {
        /// <summary>PostgreSQL <c>sqrt</c>: a negative value is a domain error.</summary>
        /// <exception cref="NotSupportedException"><paramref name="value"/> is negative.</exception>
        public static double Sqrt(double value) =>
            value < 0 ? throw new NotSupportedException("square root of a negative number") : Math.Sqrt(value);

        /// <summary>PostgreSQL <c>/</c>: division by zero is an error, and a finite input that overflows or underflows is a range error.</summary>
        /// <exception cref="NotSupportedException"><paramref name="right"/> is zero, or the quotient is out of range.</exception>
        public static double Divide(double left, double right)
        {
            if (right == 0)
            {
                throw new NotSupportedException("division by zero");
            }

            var result = left / right;
            if (double.IsInfinity(result) && !double.IsInfinity(left) && !double.IsInfinity(right))
            {
                throw new NotSupportedException("value out of range: overflow");
            }

            if (result == 0.0 && left != 0.0 && !double.IsInfinity(right))
            {
                throw new NotSupportedException("value out of range: underflow");
            }

            return result;
        }

        /// <summary>PostgreSQL <c>abs</c> of <c>int</c>: the minimum <c>int</c> overflows.</summary>
        /// <exception cref="NotSupportedException"><paramref name="value"/> is <see cref="int.MinValue"/>.</exception>
        public static int Abs(int value) =>
            value == int.MinValue ? throw new NotSupportedException("integer out of range") : Math.Abs(value);

        /// <summary>PostgreSQL <c>%</c>: modulo by zero is an error.</summary>
        /// <exception cref="NotSupportedException"><paramref name="right"/> is zero.</exception>
        public static double Modulo(double left, double right) =>
            right == 0 ? throw new NotSupportedException("division by zero") : left % right;

        /// <summary>SQL <c>SUBSTRING(s FROM start FOR length)</c>: 1-based, positions outside the string are dropped.</summary>
        /// <exception cref="NotSupportedException"><paramref name="length"/> is negative.</exception>
        public static string Substring(string source, int start, int length)
        {
            if (length < 0)
            {
                throw new NotSupportedException("negative substring length not allowed");
            }

            long from = Math.Max(start, 1);
            long to = Math.Min((long)start + length, (long)Length(source) + 1);
            if (to <= from)
            {
                return string.Empty;
            }

            var lengthPoints = (int)(to - from);
            return SliceByCodePoints(source, (int)from - 1, lengthPoints);
        }

        /// <summary>First <paramref name="length"/> code points; a negative length drops that many from the end.</summary>
        public static string Left(string source, int length)
        {
            var total = CodePointCount(source);
            var count = length >= 0 ? Math.Min(length, total) : Math.Max(total + length, 0);
            return SliceByCodePoints(source, 0, count);
        }

        /// <summary>Last <paramref name="length"/> code points; a negative length drops that many from the start.</summary>
        public static string Right(string source, int length)
        {
            var total = CodePointCount(source);
            var count = length >= 0 ? Math.Min(length, total) : Math.Max(total + length, 0);
            return SliceByCodePoints(source, total - count, count);
        }

        /// <summary>SQL <c>TRIM</c>: removes leading and trailing spaces only.</summary>
        public static string Trim(string source) => source.Trim(' ');

        /// <summary>SQL <c>LTRIM</c>: removes leading spaces only.</summary>
        public static string LTrim(string source) => source.TrimStart(' ');

        /// <summary>SQL <c>RTRIM</c>: removes trailing spaces only.</summary>
        public static string RTrim(string source) => source.TrimEnd(' ');

        /// <summary>SQL <c>REPLACE</c>: ordinal; an empty <paramref name="oldValue"/> leaves the string unchanged.</summary>
        public static string Replace(string source, string oldValue, string newValue) =>
            oldValue.Length == 0 ? source : source.Replace(oldValue, newValue);

        /// <summary>Adds <paramref name="delta"/> to a time of day, wrapping at 24 hours like SQL <c>time</c> arithmetic.</summary>
        public static TimeSpan AddTimeOfDay(TimeSpan value, TimeSpan delta)
        {
            var ticks = (value.Ticks + delta.Ticks) % TimeSpan.TicksPerDay;
            return new TimeSpan(ticks < 0 ? ticks + TimeSpan.TicksPerDay : ticks);
        }

        /// <summary>SQL <c>MIN</c>: ignores NULL items, returns NULL for none; strings compare ordinally.</summary>
        public static TResult Min<TSource, TResult>(IEnumerable<TSource> items, Func<TSource, TResult> selector) =>
            Aggregate(items, selector, preferSmaller: true);

        /// <summary>SQL <c>MAX</c>: ignores NULL items, returns NULL for none; strings compare ordinally.</summary>
        public static TResult Max<TSource, TResult>(IEnumerable<TSource> items, Func<TSource, TResult> selector) =>
            Aggregate(items, selector, preferSmaller: false);

        private static TResult Aggregate<TSource, TResult>(IEnumerable<TSource> items, Func<TSource, TResult> selector, bool preferSmaller)
        {
            var best = default(TResult);
            var found = false;
            foreach (var item in items)
            {
                var value = selector(item);
                if (value is null)
                {
                    continue;
                }

                var compared = found ? InMemorySortComparer.Instance.Compare(value, best) : 0;
                if (!found || (preferSmaller ? compared < 0 : compared > 0))
                {
                    best = value;
                    found = true;
                }
            }

            return best!;
        }
    }
}
