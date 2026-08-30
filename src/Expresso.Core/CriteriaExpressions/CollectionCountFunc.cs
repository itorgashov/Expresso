using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class CollectionCountFunc : AbstractFunction
    {
        public CollectionCountFunc(CollectionRef collection, AbstractExpression? predicate = null)
        {
            AssertNotNull(collection, nameof(collection));
            if (predicate is not null)
            {
                AssertExpressionOfTypes(predicate, nameof(predicate), typeof(bool));
            }

            Arguments.Add(collection);
            if (predicate is not null)
            {
                Arguments.Add(predicate);
            }

            ReturnType = typeof(int);
        }

        public CollectionRef Collection => (CollectionRef)Arguments[0];

        public AbstractExpression? Predicate => Arguments.Count > 1 ? Arguments[1] : null;
    }
}
