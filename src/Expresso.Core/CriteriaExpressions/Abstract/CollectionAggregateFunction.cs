using Expresso.Core.CriteriaExpressions;

namespace Expresso.Core.CriteriaExpressions.Abstract
{
    public abstract class CollectionAggregateFunction : AbstractFunction
    {
        protected CollectionAggregateFunction(CollectionRef collection, AbstractExpression selector)
        {
            AssertNotNull(collection, nameof(collection));
            AssertNotNull(selector, nameof(selector));

            Arguments.Add(collection);
            Arguments.Add(selector);
        }

        public CollectionRef Collection => (CollectionRef)Arguments[0];

        public AbstractExpression Selector => Arguments[1];

        protected static bool IsNumeric(Type type) =>
            type == typeof(byte) || type == typeof(int) || type == typeof(double);

        protected static bool IsMinMaxSelectorType(Type type)
        {
            if (IsNumeric(type) || type == typeof(string) || type == typeof(DateTime) || type == typeof(TimeSpan))
            {
                return true;
            }

#if NET6_0_OR_GREATER
            if (type == typeof(DateOnly) || type == typeof(TimeOnly))
            {
                return true;
            }
#endif
            return false;
        }
    }
}
