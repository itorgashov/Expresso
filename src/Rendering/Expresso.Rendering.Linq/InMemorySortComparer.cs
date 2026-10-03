using System.Collections;

namespace Expresso.Rendering.Linq
{
    /// <summary>
    /// In-memory key order with PostgreSQL semantics: NULL is greater than every value (last ascending, first descending)
    /// and strings compare ordinally.
    /// </summary>
    public sealed class InMemorySortComparer : IComparer<object?>
    {
        /// <summary>Shared instance.</summary>
        public static readonly InMemorySortComparer Instance = new();

        private InMemorySortComparer()
        {
        }

        /// <inheritdoc />
        public int Compare(object? x, object? y)
        {
            if (x is null)
            {
                return y is null ? 0 : 1;
            }

            if (y is null)
            {
                return -1;
            }

            return x is string sx && y is string sy
                ? string.CompareOrdinal(sx, sy)
                : Comparer.Default.Compare(x, y);
        }
    }
}
