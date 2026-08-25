namespace Expresso.Core.CriteriaExpressions.Abstract
{
    public abstract class StringSingleArgFunction : StringFunction
    {
        protected StringSingleArgFunction(AbstractExpression argument)
        {
            AssertNotNull(argument, nameof(argument));
            AssertExpressionOfTypes(argument, nameof(argument), typeof(string));
            Arguments.Add(argument);
        }
    }
}
