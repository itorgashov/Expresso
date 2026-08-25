using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.CriteriaExpressions
{
    public sealed class MinuteFunc : DateTimeSingleArgIntFunction
    {
        public MinuteFunc(AbstractExpression argument) : base(argument)
        {
        }
    }
}
