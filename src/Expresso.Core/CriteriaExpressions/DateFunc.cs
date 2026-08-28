using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class DateFunc : AbstractFunction
    {
        public DateFunc(AbstractExpression argument)
        {
            AssertNotNull(argument, nameof(argument));
#if NET6_0_OR_GREATER
            AssertExpressionOfTypes(argument, nameof(argument), typeof(DateTime), typeof(string), typeof(DateOnly));
            ReturnType = typeof(DateOnly);
#else
            AssertExpressionOfTypes(argument, nameof(argument), typeof(DateTime));
            ReturnType = typeof(DateTime);
#endif

            Arguments.Add(argument);
        }
    }
}
