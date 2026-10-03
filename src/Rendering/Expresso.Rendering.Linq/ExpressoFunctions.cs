namespace Expresso.Rendering.Linq
{
    /// <summary>
    /// In-memory reference implementations with PostgreSQL semantics, used by <see cref="InMemoryExpressionToLinqTransformer"/>.
    /// Arguments are never NULL here: NULL propagation is handled by the transformer.
    /// </summary>
    public static class ExpressoFunctions
    {
        /// <summary>Rounds half away from zero at <paramref name="digits"/> decimal places (negative digits round left of the point).</summary>
        public static double Round(double value, int digits)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) >= 7.9e27 || digits > 28)
            {
                return value;
            }

            var exact = (decimal)value;
            if (digits >= 0)
            {
                return (double)Math.Round(exact, digits, MidpointRounding.AwayFromZero);
            }

            if (digits < -28)
            {
                return 0d;
            }

            var factor = Pow10(-digits);
            return (double)(Math.Round(exact / factor, MidpointRounding.AwayFromZero) * factor);
        }

        /// <summary>SQL <c>SUBSTRING(s FROM start FOR length)</c>: 1-based, positions outside the string are dropped.</summary>
        /// <exception cref="ArgumentException"><paramref name="length"/> is negative.</exception>
        public static string Substring(string source, int start, int length)
        {
            if (length < 0)
            {
                throw new ArgumentException("negative substring length not allowed", nameof(length));
            }

            long from = Math.Max(start, 1);
            long to = Math.Min((long)start + length, (long)source.Length + 1);
            return to <= from ? string.Empty : source.Substring((int)from - 1, (int)(to - from));
        }

        /// <summary>First <paramref name="length"/> characters; a negative length drops that many characters from the end.</summary>
        public static string Left(string source, int length)
        {
            var count = length >= 0 ? Math.Min(length, source.Length) : Math.Max(source.Length + length, 0);
            return source.Substring(0, count);
        }

        /// <summary>Last <paramref name="length"/> characters; a negative length drops that many characters from the start.</summary>
        public static string Right(string source, int length)
        {
            var count = length >= 0 ? Math.Min(length, source.Length) : Math.Max(source.Length + length, 0);
            return source.Substring(source.Length - count);
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
