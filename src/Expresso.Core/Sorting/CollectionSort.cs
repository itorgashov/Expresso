namespace Expresso.Core.Sorting
{
    public sealed class CollectionSort
    {
        public CollectionSort(string name, SortDirective directive)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Collection name must not be empty.", nameof(name));
            }

            Name = name.ToLowerInvariant();
            Directive = directive ?? throw new ArgumentNullException(nameof(directive));
        }

        public string Name { get; }

        public SortDirective Directive { get; }
    }
}
