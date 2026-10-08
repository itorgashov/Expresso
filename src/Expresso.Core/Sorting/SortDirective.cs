using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.Sorting
{
    public class SortDirective
    {
        private readonly List<SortDirectiveItem> _items;
        private readonly List<CollectionSort> _nested;

        public SortDirective(IEnumerable<SortDirectiveItem> items)
            : this(items, Array.Empty<CollectionSort>())
        {
        }

        public SortDirective(IEnumerable<SortDirectiveItem> items, IReadOnlyList<CollectionSort> nested)
        {
            _items = items?.ToList() ?? throw new ArgumentNullException(nameof(items));
            _nested = nested?.ToList() ?? throw new ArgumentNullException(nameof(nested));
        }

        public IReadOnlyList<SortDirectiveItem> Items => _items.AsReadOnly();

        public IReadOnlyList<CollectionSort> Nested => _nested.AsReadOnly();

        public SortDirective RemoveDuplicates()
        {
            var uniqueItems = DeduplicateItems(_items);
            var uniqueNested = DeduplicateNested(_nested);
            if (uniqueItems.Count == _items.Count && uniqueNested.Count == _nested.Count)
            {
                return this;
            }

            return new SortDirective(uniqueItems, uniqueNested);
        }

        public int TotalSortKeyCount() => _items.Count + SumNestedKeyCount(_nested);

        /// <summary>Returns a new directive with <paramref name="expression"/> appended. <see cref="Nested"/> is kept and this instance is unchanged.</summary>
        /// <param name="expression">Sort key to append.</param>
        /// <param name="direction">Direction of the appended key.</param>
        /// <returns>The new directive.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <see langword="null"/>.</exception>
        public SortDirective ThenBy(AbstractExpression expression, SortDirection direction = SortDirection.Ascending)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var items = new List<SortDirectiveItem>(_items.Count + 1);
            items.AddRange(_items);
            items.Add(new SortDirectiveItem { Expression = expression, Direction = direction });
            return new SortDirective(items, _nested);
        }

        private static int SumNestedKeyCount(IReadOnlyList<CollectionSort> nested)
        {
            var count = 0;
            foreach (var collectionSort in nested)
            {
                count += collectionSort.Directive.TotalSortKeyCount();
            }

            return count;
        }

        private static List<SortDirectiveItem> DeduplicateItems(IReadOnlyList<SortDirectiveItem> items)
        {
            if (items.Count < 2)
            {
                return items.ToList();
            }

            var uniqueExpressions = new HashSet<AbstractExpression>();
            var uniqueItems = new List<SortDirectiveItem>(items.Count);
            foreach (var item in items)
            {
                if (uniqueExpressions.Add(item.Expression))
                {
                    uniqueItems.Add(item);
                }
            }

            return uniqueItems;
        }

        private static List<CollectionSort> DeduplicateNested(IReadOnlyList<CollectionSort> nested)
        {
            if (nested.Count == 0)
            {
                return new List<CollectionSort>();
            }

            var result = new List<CollectionSort>(nested.Count);
            foreach (var collectionSort in nested)
            {
                var dedupedDirective = collectionSort.Directive.RemoveDuplicates();
                result.Add(new CollectionSort(collectionSort.Name, dedupedDirective));
            }

            return result;
        }
    }
}
