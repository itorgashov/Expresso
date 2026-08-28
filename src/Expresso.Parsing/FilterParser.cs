using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;

namespace Expresso.Parsing
{
    public class FilterParser : IFilterParser
    {
        private readonly ExpressionParser _expressionParser;

        public FilterParser() : this(LiteralParseOptions.Default)
        {
        }

        public FilterParser(LiteralParseOptions options)
        {
            _expressionParser = new ExpressionParser(options ?? LiteralParseOptions.Default);
        }

        public FilterCriteria Parse(string query, (string, Type)[] validFields)
        {
            AbstractExpression? parsedExpression = _expressionParser.Parse(query, validFields);

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
