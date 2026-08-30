namespace Expresso.SqlServer
{
    public sealed class CollectionSqlMapping
    {
        public CollectionSqlMapping(
            string name,
            string fromClause,
            string correlateSql,
            Dictionary<string, string> itemFieldToColumn,
            IEnumerable<CollectionSqlMapping>? nested = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Collection mapping name must not be empty.", nameof(name));
            }

            Name = name.ToLowerInvariant();
            FromClause = fromClause ?? throw new ArgumentNullException(nameof(fromClause));
            CorrelateSql = correlateSql ?? throw new ArgumentNullException(nameof(correlateSql));
            ItemFieldToColumn = ToCaseInsensitive(itemFieldToColumn ?? throw new ArgumentNullException(nameof(itemFieldToColumn)));
            Nested = ToNamedDictionary(nested);
        }

        public string Name { get; }

        public string FromClause { get; }

        public string CorrelateSql { get; }

        public Dictionary<string, string> ItemFieldToColumn { get; }

        public Dictionary<string, CollectionSqlMapping> Nested { get; }

        internal static Dictionary<string, string> ToCaseInsensitive(Dictionary<string, string> source)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in source)
            {
                result[pair.Key] = pair.Value;
            }

            return result;
        }

        internal static Dictionary<string, CollectionSqlMapping> ToNamedDictionary(IEnumerable<CollectionSqlMapping>? nested)
        {
            var result = new Dictionary<string, CollectionSqlMapping>(StringComparer.OrdinalIgnoreCase);
            if (nested is null)
            {
                return result;
            }

            foreach (var mapping in nested)
            {
                result[mapping.Name] = mapping;
            }

            return result;
        }
    }

    public sealed class SqlQueryMapping
    {
        public SqlQueryMapping(
            Dictionary<string, string> fieldToColumn,
            IEnumerable<CollectionSqlMapping>? collections = null)
        {
            FieldToColumn = CollectionSqlMapping.ToCaseInsensitive(
                fieldToColumn ?? throw new ArgumentNullException(nameof(fieldToColumn)));
            Collections = CollectionSqlMapping.ToNamedDictionary(collections);
        }

        public Dictionary<string, string> FieldToColumn { get; }

        public Dictionary<string, CollectionSqlMapping> Collections { get; }
    }
}
