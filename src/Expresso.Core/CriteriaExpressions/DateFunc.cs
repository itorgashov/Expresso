using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class DateFunc : AbstractFunction
    {
        public DateFunc(AbstractExpression argument)
        {
            AssertNotNull(argument, nameof(argument));
            AssertExpressionOfTypes(argument, nameof(argument), typeof(DateTime));

            Arguments.Add(argument);
            ReturnType = typeof(DateTime);
        }
    }
}
