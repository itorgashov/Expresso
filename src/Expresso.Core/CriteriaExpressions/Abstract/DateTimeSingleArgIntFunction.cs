namespace Expresso.Core.CriteriaExpressions.Abstract
{
    public abstract class DateTimeSingleArgIntFunction : AbstractFunction
    {
        protected DateTimeSingleArgIntFunction(AbstractExpression argument)
        {
            AssertNotNull(argument, nameof(argument));
            AssertExpressionOfTypes(argument, nameof(argument), typeof(DateTime));

            Arguments.Add(argument);
            ReturnType = typeof(int);
        }
    }
}
