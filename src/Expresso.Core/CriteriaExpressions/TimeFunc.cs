#if NET6_0_OR_GREATER
using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class TimeFunc : AbstractFunction
    {
        public TimeFunc(AbstractExpression argument)
        {
            AssertNotNull(argument, nameof(argument));
            AssertExpressionOfTypes(argument, nameof(argument), typeof(DateTime), typeof(string), typeof(TimeOnly));

            Arguments.Add(argument);
            ReturnType = typeof(TimeOnly);
        }
    }
}
#endif
