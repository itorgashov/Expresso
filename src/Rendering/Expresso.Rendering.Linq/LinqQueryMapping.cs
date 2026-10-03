using System.Linq.Expressions;

namespace Expresso.Rendering.Linq
{
    /// <summary>
    /// Maps query field names to member lambdas and collection names to navigation lambdas for one entity type.
    /// Lookup is case-insensitive, like <c>SqlQueryMapping</c>.
    /// </summary>
    public abstract class LinqQueryMapping
    {
        private readonly Dictionary<string, LambdaExpression> _fields = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, LinqCollectionMapping> _collections = new(StringComparer.OrdinalIgnoreCase);

        private protected LinqQueryMapping(Type entityType)
        {
            EntityType = entityType;
        }

        /// <summary>Entity type the lambdas take as their parameter.</summary>
        public Type EntityType { get; }

        /// <summary>Field name to member lambda (<c>e =&gt; e.Member</c>).</summary>
        public IReadOnlyDictionary<string, LambdaExpression> Fields => _fields;

        /// <summary>Collection name to navigation mapping.</summary>
        public IReadOnlyDictionary<string, LinqCollectionMapping> Collections => _collections;

        /// <summary>Resolves a nested collection mapping by path (for example <c>tags</c>, <c>tag_meta</c>).</summary>
        /// <returns>The mapping, or <see langword="null"/> when a segment is not mapped.</returns>
        public LinqCollectionMapping? ResolveCollection(params string[] path)
        {
            if (path is null || path.Length == 0)
            {
                return null;
            }

            LinqQueryMapping current = this;
            LinqCollectionMapping? found = null;
            foreach (var segment in path)
            {
                if (!current.Collections.TryGetValue(segment, out found))
                {
                    return null;
                }

                current = found.Items;
            }

            return found;
        }

        private protected void AddField(string name, LambdaExpression selector)
        {
            EnsureName(name);
            _fields[name] = selector ?? throw new ArgumentNullException(nameof(selector));
        }

        private protected void AddCollection(string name, LambdaExpression navigation, LinqQueryMapping items)
        {
            EnsureName(name);
            if (navigation is null)
            {
                throw new ArgumentNullException(nameof(navigation));
            }

            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            _collections[name] = new LinqCollectionMapping(name.ToLowerInvariant(), navigation, items);
        }

        private static void EnsureName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Mapping name must not be empty.", nameof(name));
            }
        }
    }

    /// <summary>Fluent LINQ mapping for entity type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">Entity type.</typeparam>
    public sealed class LinqQueryMapping<T> : LinqQueryMapping
    {
        /// <summary>Creates an empty mapping.</summary>
        public LinqQueryMapping() : base(typeof(T))
        {
        }

        /// <summary>Maps a query field to a member lambda.</summary>
        /// <typeparam name="TValue">Must be the catalog type of the field or its <see cref="Nullable{T}"/>.</typeparam>
        /// <param name="name">Query field name.</param>
        /// <param name="selector">Member lambda, for example <c>b =&gt; b.Title</c>.</param>
        /// <returns>This mapping.</returns>
        public LinqQueryMapping<T> Field<TValue>(string name, Expression<Func<T, TValue>> selector)
        {
            AddField(name, selector);
            return this;
        }

        /// <summary>Maps a query collection to a navigation lambda and the mapping of its items.</summary>
        /// <typeparam name="TItem">Item type.</typeparam>
        /// <param name="name">Collection name used in filters and <c>sortfor</c>. Stored in lowercase.</param>
        /// <param name="navigation">Navigation lambda, for example <c>b =&gt; b.Authors</c>.</param>
        /// <param name="items">Mapping for one item.</param>
        /// <returns>This mapping.</returns>
        public LinqQueryMapping<T> Collection<TItem>(
            string name,
            Expression<Func<T, IEnumerable<TItem>>> navigation,
            LinqQueryMapping<TItem> items)
        {
            AddCollection(name, navigation, items);
            return this;
        }
    }

    /// <summary>One mapped collection: navigation lambda plus the mapping of its items.</summary>
    public sealed class LinqCollectionMapping
    {
        internal LinqCollectionMapping(string name, LambdaExpression navigation, LinqQueryMapping items)
        {
            Name = name;
            Navigation = navigation;
            Items = items;
        }

        /// <summary>Collection name, stored in lowercase.</summary>
        public string Name { get; }

        /// <summary>Navigation lambda from the parent entity to the item sequence.</summary>
        public LambdaExpression Navigation { get; }

        /// <summary>Mapping for one item.</summary>
        public LinqQueryMapping Items { get; }
    }
}
