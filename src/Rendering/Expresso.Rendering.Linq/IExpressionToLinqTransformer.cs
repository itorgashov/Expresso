using System.Linq.Expressions;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.Rendering.Linq
{
    /// <summary>Renders Expresso filters and sort directives to LINQ lambdas.</summary>
    public interface IExpressionToLinqTransformer
    {
        /// <summary>Builds <c>e =&gt; predicate</c>. Rows where the filter is unknown (SQL NULL) are excluded.</summary>
        /// <exception cref="ArgumentException">A field or collection has no mapping, or a mapped type does not match the catalog.</exception>
        Expression<Func<T, bool>> BuildPredicate<T>(FilterCriteria filter, LinqQueryMapping<T> mapping);

        /// <summary>Builds the parent sort keys of <paramref name="sort"/> (nested <c>sortfor</c> directives are not included).</summary>
        /// <exception cref="ArgumentException">The directive is empty, uses a collection quantifier, or references an unmapped field.</exception>
        IReadOnlyList<LinqSortKey> BuildSortKeys<T>(SortDirective sort, LinqQueryMapping<T> mapping);
    }
}
