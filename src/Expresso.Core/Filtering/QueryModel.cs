namespace Expresso.Core.Filtering
{
    public sealed class QueryModel
    {
        public static QueryModel Empty { get; } = new QueryModel(Array.Empty<(string, Type)>());

        private readonly Dictionary<string, Type> _fields;
        private readonly Dictionary<string, CollectionModel> _collections;

        public QueryModel((string, Type)[]? fields, IReadOnlyList<CollectionModel>? collections = null)
        {
            _fields = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            _collections = new Dictionary<string, CollectionModel>(StringComparer.OrdinalIgnoreCase);

            if (fields is not null)
            {
                foreach (var (name, type) in fields)
                {
                    if (name is null || type is null || string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    var key = name.ToLowerInvariant();
                    if (_fields.ContainsKey(key))
                    {
                        continue;
                    }

                    _fields[key] = type;
                }
            }

            if (collections is not null)
            {
                foreach (var collection in collections)
                {
                    if (collection is null)
                    {
                        continue;
                    }

                    if (_collections.ContainsKey(collection.Name))
                    {
                        throw new ArgumentException($"Duplicate collection name: '{collection.Name}'.");
                    }

                    if (_fields.ContainsKey(collection.Name))
                    {
                        throw new ArgumentException($"Name '{collection.Name}' is both a field and a collection.");
                    }

                    _collections[collection.Name] = collection;
                }
            }

            Fields = _fields.Select(pair => (pair.Key, pair.Value)).ToArray();
            Collections = _collections.Values.ToArray();
        }

        public (string, Type)[] Fields { get; }

        public CollectionModel[] Collections { get; }

        public static QueryModel FromFields((string, Type)[] fields)
        {
            if (fields is null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            return new QueryModel(fields);
        }

        public bool TryGetField(string name, out Type type)
        {
            type = null!;
            return name is not null && _fields.TryGetValue(name, out type);
        }

        public bool TryGetCollection(string name, out CollectionModel collection)
        {
            collection = null!;
            return name is not null && _collections.TryGetValue(name, out collection);
        }
    }
}
