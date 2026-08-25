namespace Expresso.Core.CriteriaExpressions.Abstract
{
    public abstract class DateTimeAddFunction : AbstractFunction
    {
        protected DateTimeAddFunction(AbstractExpression dateTime, AbstractExpression amount)
        {
            AssertNotNull(dateTime, nameof(dateTime));
            AssertExpressionOfTypes(dateTime, nameof(dateTime), typeof(DateTime));
            AssertNotNull(amount, nameof(amount));
            AssertExpressionOfTypes(amount, nameof(amount), typeof(int));

            Arguments.Add(dateTime);
            Arguments.Add(amount);
            ReturnType = typeof(DateTime);
        }
    }
}
