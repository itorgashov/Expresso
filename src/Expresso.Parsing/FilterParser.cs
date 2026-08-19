using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;

namespace Expresso.Parsing
{
    public class FilterParser : IFilterParser
    {
        public FilterCriteria Parse(string query, (string, Type)[] validFields)
        {
            ExpressionParser parser = ExpressionParser.GetInstance();
            AbstractExpression? parsedExpression = parser.Parse(query, validFields);

            if (parsedExpression is BooleanFunction fn)
            {
                return new FilterCriteria()
                {
                    Expression = fn
                };
            }
            else
            {
                throw new ArgumentException("A boolean expression is expected.");
            }
        }
    }
}
