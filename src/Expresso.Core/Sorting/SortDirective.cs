using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.Sorting
{
    public class SortDirective
    {
        private readonly List<SortDirectiveItem> _items;

        public SortDirective(IEnumerable<SortDirectiveItem> items)
        {
            _items = items?.ToList() ?? throw new ArgumentNullException(nameof(items));
        }

        public IReadOnlyList<SortDirectiveItem> Items => _items.AsReadOnly();

        public SortDirective RemoveDuplicates()
        {
            if (_items.Count < 2)
                return this;

            var uniqueExpressions = new HashSet<AbstractExpression>();
            var uniqueItems = new List<SortDirectiveItem>(_items.Count);

            foreach (var item in _items)
            {
                if (uniqueExpressions.Add(item.Expression))
                {
                    uniqueItems.Add(item);
                }
            }

            return uniqueItems.Count == _items.Count
                ? this
                : new SortDirective(uniqueItems);
        }
    }
}
