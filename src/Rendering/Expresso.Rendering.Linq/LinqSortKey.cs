using System.Linq.Expressions;
using Expresso.Core.Sorting;

namespace Expresso.Rendering.Linq
{
    /// <summary>One sort key: a key lambda (<c>e =&gt; key</c>) and its direction.</summary>
    public sealed class LinqSortKey
    {
        /// <summary>Creates a sort key.</summary>
        public LinqSortKey(LambdaExpression key, SortDirection direction)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Direction = direction;
        }

        /// <summary>Key lambda. Nullable keys return <see cref="Nullable{T}"/> or a reference type.</summary>
        public LambdaExpression Key { get; }

        /// <summary>Sort direction.</summary>
        public SortDirection Direction { get; }
    }
}
