using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Sorting;

namespace Expresso.Parsing
{
    public class SortDirectiveParser : ISortDirectiveParser
    {
        public SortDirective Parse(string query, (string, Type)[] validFields)
        {
            List<SortDirectiveItem> items = new List<SortDirectiveItem>();
            var unparsedExpressions = SplitToExpressionList(query);
            AbstractExpression? expr = default!;
            SortDirection dir;

            ExpressionParser parser = ExpressionParser.GetInstance();
            for (int i = 0; i < unparsedExpressions.Count; i++)
            {
                if (i % 2 == 0)
                {
                    expr = parser.Parse(unparsedExpressions[i], validFields);
                    if (expr is null)
                    {
                        throw new ArgumentException($"Failed to parse directive [{i / 2 + 1}]");
                    }
                }
                else
                {
                    switch (unparsedExpressions[i].ToLower())
                    {
                        case "asc":
                            dir = SortDirection.Ascending;
                            break;
                        case "desc":
                            dir = SortDirection.Descending;
                            break;
                        default:
                            throw new NotSupportedException($"Unrecodnized sorting direction marker: {unparsedExpressions[i]}.");
                    }
                    items.Add(new SortDirectiveItem()
                    {
                        Expression = expr,
                        Direction = dir
                    });
                }
            }
            if (unparsedExpressions.Count % 2 == 1 || unparsedExpressions.Count == 0)
            {
                throw new ArgumentException($"Unexpected end of sort directive");
            }

            return new SortDirective(items);
        }

        private List<string> SplitToExpressionList(string input)
        {
            var tokens = new List<string>();
            int start = 0;
            int depth = 0;

            for (int i = 0; i < input.Length; i++)
            {
                char currentChar = input[i];

                if (currentChar == '(')
                {
                    depth++;
                }
                else if (currentChar == ')')
                {
                    depth--;
                }
                else if (currentChar == ',' && depth == 0)
                {
                    tokens.Add(input.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }

            if (start < input.Length)
            {
                tokens.Add(input.Substring(start).Trim());
            }

            return tokens;
        }
    }
}
