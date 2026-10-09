using Expresso.Core.CriteriaExpressions;
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

        /// <summary>Parses a filter using the supplied fields.</summary>
        /// <remarks>This overload has no policy. Pass a policy-bearing <see cref="QueryModel"/> to the other overload for enforcement.</remarks>
        public FilterCriteria Parse(string query, (string, Type)[] validFields)
        {
            if (validFields is null)
            {
                throw new ArgumentNullException(nameof(validFields));
            }

            return Parse(query, QueryModel.FromFields(validFields));
        }

        public FilterCriteria Parse(string query, QueryModel queryModel)
        {
            AbstractExpression? parsedExpression = _expressionParser.Parse(query, queryModel);

            if (parsedExpression is BooleanFunction fn)
            {
                if (queryModel.Policy is not null)
                    Policies.Runtime.PolicyEnforcer.EnforceFilter(queryModel, fn);
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
