using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.Parsing
{
    public partial class SortDirectiveParser : ISortDirectiveParser
    {
        private readonly ExpressionParser _expressionParser;

        public SortDirectiveParser() : this(LiteralParseOptions.Default)
        {
        }

        public SortDirectiveParser(LiteralParseOptions options)
        {
            _expressionParser = new ExpressionParser(options ?? LiteralParseOptions.Default);
        }

        public SortDirective Parse(string query, (string, Type)[] validFields)
        {
            if (validFields is null)
            {
                throw new ArgumentNullException(nameof(validFields));
            }

            return Parse(query, QueryModel.FromFields(validFields));
        }

        public SortDirective Parse(string query, QueryModel queryModel)
        {
            List<SortDirectiveItem> items = new List<SortDirectiveItem>();
            var nestedBuilder = new NestedSortBuilder();
            var unparsedExpressions = SplitToExpressionList(query);
            AbstractExpression? expr = default!;

            for (int i = 0; i < unparsedExpressions.Count; i++)
            {
                if (i % 2 == 0)
                {
                    var chunk = unparsedExpressions[i];
                    if (TryParseSortForChunk(chunk, queryModel, out var collectionPath, out var sortForExpr))
                    {
                        expr = sortForExpr;
                        i++;
                        if (i >= unparsedExpressions.Count)
                        {
                            throw new ArgumentException("Unexpected end of sort directive");
                        }

                        var direction = ParseDirection(unparsedExpressions[i]);
                        AddNestedSort(nestedBuilder, collectionPath, new SortDirectiveItem
                        {
                            Expression = expr,
                            Direction = direction,
                        });
                        continue;
                    }

                    expr = _expressionParser.Parse(unparsedExpressions[i], queryModel);
                    if (expr is null)
                    {
                        throw new ArgumentException($"Failed to parse directive [{i / 2 + 1}]");
                    }

                    if (expr is CollectionQuantifierFunction or CollectionRef)
                    {
                        throw new ArgumentException("Collections and collection quantifiers cannot be used as sort keys.");
                    }
                }
                else
                {
                    items.Add(new SortDirectiveItem()
                    {
                        Expression = expr,
                        Direction = ParseDirection(unparsedExpressions[i]),
                    });
                }
            }
            if (unparsedExpressions.Count % 2 == 1 || unparsedExpressions.Count == 0)
            {
                throw new ArgumentException($"Unexpected end of sort directive");
            }

            var nested = nestedBuilder.Children
                .Select(kv => new CollectionSort(kv.Key, kv.Value.Build()))
                .ToList();
            return new SortDirective(items, nested);
        }

        private static SortDirection ParseDirection(string token)
        {
            switch (token.ToLower())
            {
                case "asc":
                    return SortDirection.Ascending;
                case "desc":
                    return SortDirection.Descending;
                default:
                    throw new NotSupportedException($"Unrecodnized sorting direction marker: {token}.");
            }
        }

        private static List<string> SplitToExpressionList(string input)
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
