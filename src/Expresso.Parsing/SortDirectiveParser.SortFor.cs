using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.Parsing
{
    public partial class SortDirectiveParser
    {
        private sealed class NestedSortBuilder
        {
            public List<SortDirectiveItem> Items { get; } = new();

            public Dictionary<string, NestedSortBuilder> Children { get; } =
                new(StringComparer.OrdinalIgnoreCase);

            public SortDirective Build()
            {
                var nested = Children
                    .Select(kv => new CollectionSort(kv.Key, kv.Value.Build()))
                    .ToList();
                return new SortDirective(Items, nested);
            }
        }

        private bool TryParseSortForChunk(
            string chunk,
            QueryModel queryModel,
            out string[] collectionPath,
            out AbstractExpression expression)
        {
            collectionPath = Array.Empty<string>();
            expression = null!;

            if (!chunk.StartsWith("sortfor(", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var inner = ExtractBalancedInner(chunk, "sortfor(".Length);
            if (inner is null)
            {
                throw new ArgumentException("Unexpected end of sort directive.");
            }

            if (!TrySplitAtTopLevelComma(inner, out var pathPart, out var expressionPart))
            {
                throw new ArgumentException("sortfor() requires exactly 2 arguments.");
            }

            collectionPath = ParseCollectionPath(pathPart);
            var (itemModel, scopePath) = ResolveCollectionPath(queryModel, collectionPath);
            expression = _expressionParser.Parse(expressionPart, itemModel, scopePath)
                ?? throw new ArgumentException("Failed to parse sortfor expression.");

            if (expression is CollectionQuantifierFunction or CollectionRef)
            {
                throw new ArgumentException("Collections and collection quantifiers cannot be used as sort keys.");
            }

            return true;
        }

        private static string? ExtractBalancedInner(string chunk, int startIndex)
        {
            var depth = 1;
            for (var i = startIndex; i < chunk.Length; i++)
            {
                if (chunk[i] == '(')
                {
                    depth++;
                }
                else if (chunk[i] == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return chunk.Substring(startIndex, i - startIndex).Trim();
                    }
                }
            }

            return null;
        }

        private static bool TrySplitAtTopLevelComma(string inner, out string left, out string right)
        {
            var depth = 0;
            for (var i = 0; i < inner.Length; i++)
            {
                var c = inner[i];
                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;
                }
                else if (c == ',' && depth == 0)
                {
                    left = inner.Substring(0, i).Trim();
                    right = inner.Substring(i + 1).Trim();
                    return !string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right);
                }
            }

            left = string.Empty;
            right = string.Empty;
            return false;
        }

        private static string[] ParseCollectionPath(string pathPart)
        {
            if (string.IsNullOrWhiteSpace(pathPart))
            {
                throw new ArgumentException("sortfor() collection path must not be empty.");
            }

            if (pathPart.StartsWith("/"))
            {
                throw new ArgumentException("sortfor() collection path must not start with '/'.");
            }

            var segments = pathPart.Split('/');
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i].Trim();
                if (string.IsNullOrWhiteSpace(segment))
                {
                    throw new ArgumentException("sortfor() collection path contains an empty segment.");
                }

                segments[i] = segment.ToLowerInvariant();
            }

            return segments;
        }

        private static (QueryModel ItemModel, string? ScopePath) ResolveCollectionPath(
            QueryModel rootModel,
            string[] collectionPath)
        {
            var currentModel = rootModel;
            string? scopePath = null;

            foreach (var segment in collectionPath)
            {
                if (!currentModel.TryGetCollection(segment, out var collection))
                {
                    throw new ArgumentException($"Illegal field name: '{segment}'.");
                }

                scopePath = scopePath is null ? segment : scopePath + "." + segment;
                currentModel = collection.Items;
            }

            return (currentModel, scopePath);
        }

        private static void AddNestedSort(
            NestedSortBuilder builder,
            string[] collectionPath,
            SortDirectiveItem item)
        {
            var node = builder;
            foreach (var segment in collectionPath)
            {
                if (!node.Children.TryGetValue(segment, out var child))
                {
                    child = new NestedSortBuilder();
                    node.Children[segment] = child;
                }

                node = child;
            }

            node.Items.Add(item);
        }
    }
}
