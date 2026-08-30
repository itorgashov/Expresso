using Expresso.Core.CriteriaExpressions;

namespace Expresso.Core.CriteriaExpressions.Abstract
{
    public abstract class CollectionQuantifierFunction : BooleanFunction
    {
        protected CollectionQuantifierFunction(CollectionRef collection, AbstractExpression? predicate)
        {
            AssertNotNull(collection, nameof(collection));
            if (collection.ReturnType != typeof(CollectionRef))
            {
                throw new ArgumentException("First argument must be a collection.", nameof(collection));
            }

            if (predicate is not null)
            {
                AssertExpressionOfTypes(predicate, nameof(predicate), typeof(bool));
            }

            Arguments.Add(collection);
            if (predicate is not null)
            {
                Arguments.Add(predicate);
            }
        }

        public CollectionRef Collection => (CollectionRef)Arguments[0];

        public AbstractExpression? Predicate => Arguments.Count > 1 ? Arguments[1] : null;
    }
}
