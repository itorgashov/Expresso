using System.Linq.Expressions;

namespace Expresso.Rendering.Linq
{
    /// <summary>State for rendering one expression scope: the current item parameter and its mapping.</summary>
    public sealed class LinqScope
    {
        internal LinqScope(ParameterExpression item, LinqQueryMapping mapping)
        {
            Item = item;
            Mapping = mapping;
        }

        /// <summary>Lambda parameter for the current entity or collection item.</summary>
        public ParameterExpression Item { get; }

        /// <summary>Mapping for <see cref="Item"/>.</summary>
        public LinqQueryMapping Mapping { get; }
    }
}
