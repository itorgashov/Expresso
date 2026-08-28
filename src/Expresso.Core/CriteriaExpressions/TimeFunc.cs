using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class TimeFunc : AbstractFunction
    {
        public TimeFunc(AbstractExpression argument)
        {
            AssertNotNull(argument, nameof(argument));
#if NET6_0_OR_GREATER
            AssertExpressionOfTypes(argument, nameof(argument), typeof(DateTime), typeof(string), typeof(TimeOnly));
            ReturnType = typeof(TimeOnly);
#else
            AssertExpressionOfTypes(argument, nameof(argument), typeof(DateTime), typeof(string), typeof(TimeSpan));
            ReturnType = typeof(TimeSpan);
#endif

            Arguments.Add(argument);
        }
    }
}
