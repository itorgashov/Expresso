namespace Expresso.Core.CriteriaExpressions.Abstract
{
    public abstract class DateTimeAddFunction : AbstractFunction
    {
        protected DateTimeAddFunction(AbstractExpression dateTime, AbstractExpression amount, params Type[] allowedDateTypes)
        {
            AssertNotNull(dateTime, nameof(dateTime));
            AssertExpressionOfTypes(dateTime, nameof(dateTime), allowedDateTypes);
            AssertNotNull(amount, nameof(amount));
            AssertExpressionOfTypes(amount, nameof(amount), typeof(int));

            Arguments.Add(dateTime);
            Arguments.Add(amount);
            ReturnType = dateTime.ReturnType;
        }
    }
}
