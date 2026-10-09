using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Parsing.Policies.Binding;
using Expresso.Parsing.Policies.Catalog;
using Expresso.Parsing.Policies.Syntax;

namespace Expresso.Parsing.Policies.Runtime;

internal static class FailureLocator
{
    internal static string Name(AbstractExpression node) => node switch
    {
        Field field => field.Scope == null ? field.Name : field.Scope + "." + field.Name,
        CollectionRef collection => collection.Scope == null ? collection.Name : collection.Scope + "." + collection.Name,
        Literal => "literal",
        _ => FunctionCatalog.ByType.TryGetValue(node.GetType(), out var info) ? info.Names[0] : node.GetType().Name
    };

    internal static string Locate(AbstractExpression root, IEnumerable<BoundPattern> starts, PolicyEvaluator evaluator)
    {
        string best = Name(root); int bestDepth = 1;
        var pending = new Stack<(AbstractExpression Node, BoundPattern Pattern, string Path, int Depth)>();
        foreach (var start in starts.Reverse()) pending.Push((root, start, best, 1));
        var visited = new HashSet<(AbstractExpression, int)>();
        while (pending.Count != 0)
        {
            var (node, pattern, path, depth) = pending.Pop();
            if (!visited.Add((node, pattern.Id)) || evaluator.Facts[node].Bits.Has(pattern.Id)) continue;
            if (depth > bestDepth) { best = path; bestDepth = depth; }
            if (pattern.Kind is PatternKind.Reference or PatternKind.Union)
            {
                foreach (var child in pattern.Children.AsEnumerable().Reverse()) pending.Push((node, child, path, depth));
                continue;
            }
            if (pattern.Kind != PatternKind.Call || node is not AbstractFunction function || node.GetType() != pattern.Function!.NodeType) continue;
            for (int i = function.Arguments.Count - 1; i >= 0; i--)
            {
                var childPattern = pattern.Shape == ArgumentShape.Exists ? pattern.Children[0] :
                    i < pattern.Children.Count ? pattern.Children[i] :
                    pattern.TailPositions.Count != 0 ? pattern.TailAt(i - pattern.Children.Count) : null;
                if (childPattern == null) continue;
                var child = function.Arguments[i];
                pending.Push((child, childPattern, path + " > " + Name(child), depth + 1));
            }
        }
        return best;
    }
}
