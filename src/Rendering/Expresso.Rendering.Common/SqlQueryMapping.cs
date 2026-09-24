namespace Expresso.Rendering
{
    /// <summary>
    /// Maps a collection path to a SQL FROM fragment, correlate predicate, and item columns.
    /// Nested collections are used for <c>any(authors, any(awards, …))</c> and similar.
    /// </summary>
    public sealed class CollectionSqlMapping
    {
        /// <summary>Creates a mapping for one related collection.</summary>
        /// <param name="name">Collection name used in filters and <c>sortfor</c>. Stored in lowercase.</param>
        /// <param name="fromClause">SQL <c>FROM</c> fragment for the related rows, including joins. Dialect-specific SQL is allowed.</param>
        /// <param name="correlateSql">Predicate that ties a related row back to the outer query.</param>
        /// <param name="itemFieldToColumn">Item field name to column expression. Lookup is case-insensitive.</param>
        /// <param name="nested">Collections reachable from one item, or <see langword="null"/> when there are none.</param>
        /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="fromClause"/>, <paramref name="correlateSql"/>, or <paramref name="itemFieldToColumn"/> is <see langword="null"/>.</exception>
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

        /// <summary>Collection name, stored in lowercase.</summary>
        public string Name { get; }

        /// <summary>SQL <c>FROM</c> fragment for the related rows, including joins.</summary>
        public string FromClause { get; }

        /// <summary>Predicate that ties a related row back to the outer query.</summary>
        public string CorrelateSql { get; }

        /// <summary>Item field name to column expression. Lookup is case-insensitive.</summary>
        public Dictionary<string, string> ItemFieldToColumn { get; }

        /// <summary>Nested collections keyed by name. Lookup is case-insensitive.</summary>
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

    /// <summary>
    /// Root SQL mapping for scalar columns plus optional nested collections.
    /// </summary>
    public sealed class SqlQueryMapping
    {
        /// <summary>Creates the root mapping for one query.</summary>
        /// <param name="fieldToColumn">Scalar field name to column expression. Lookup is case-insensitive.</param>
        /// <param name="collections">Related collections, or <see langword="null"/> when the query has none.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fieldToColumn"/> is <see langword="null"/>.</exception>
        public SqlQueryMapping(
            Dictionary<string, string> fieldToColumn,
            IEnumerable<CollectionSqlMapping>? collections = null)
        {
            FieldToColumn = CollectionSqlMapping.ToCaseInsensitive(
                fieldToColumn ?? throw new ArgumentNullException(nameof(fieldToColumn)));
            Collections = CollectionSqlMapping.ToNamedDictionary(collections);
        }

        /// <summary>Scalar field name to column expression. Lookup is case-insensitive.</summary>
        public Dictionary<string, string> FieldToColumn { get; }

        /// <summary>Related collections keyed by name. Lookup is case-insensitive.</summary>
        public Dictionary<string, CollectionSqlMapping> Collections { get; }
    }
}
