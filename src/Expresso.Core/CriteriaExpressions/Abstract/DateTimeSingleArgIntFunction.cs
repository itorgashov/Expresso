namespace Expresso.Core.CriteriaExpressions.Abstract
{
    public abstract class DateTimeSingleArgIntFunction : AbstractFunction
    {
        protected DateTimeSingleArgIntFunction(AbstractExpression argument, params Type[] allowedTypes)
        {
            AssertNotNull(argument, nameof(argument));
            AssertExpressionOfTypes(argument, nameof(argument), allowedTypes);

            Arguments.Add(argument);
            ReturnType = typeof(int);
        }
    }
}
