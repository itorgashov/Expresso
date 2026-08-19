using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Core.Sorting
{
    public class SortDirectiveItem
    {
        public AbstractExpression Expression { get; init; }
        public SortDirection Direction { get; init; }
    }
}