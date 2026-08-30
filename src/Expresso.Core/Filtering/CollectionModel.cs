namespace Expresso.Core.Filtering
{
    public sealed class CollectionModel
    {
        public CollectionModel(string name, QueryModel items)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Collection name must not be empty.", nameof(name));
            }

            Name = name.ToLowerInvariant();
            Items = items ?? throw new ArgumentNullException(nameof(items));
        }

        public string Name { get; }

        public QueryModel Items { get; }
    }
}
