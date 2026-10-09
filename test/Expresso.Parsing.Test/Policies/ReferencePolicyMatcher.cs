using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Policies;
using Expresso.Parsing.Policies.Binding;
using Expresso.Parsing.Policies.Catalog;
using Expresso.Parsing.Policies.Runtime;
using Expresso.Parsing.Policies.Syntax;

namespace Expresso.Parsing.Test.Policies;

// Top-down interpreter of the bound graph. Independent of evaluator bitsets,
// but deliberately shares binder output; AstPolicyMatcher checks binding separately.
internal sealed class ReferencePolicyMatcher
{
    private readonly QueryPolicyLimits _limits;
    private readonly HashSet<(AbstractExpression, BoundPattern)> _active = new();
    internal ReferencePolicyMatcher(QueryPolicyLimits limits) { _limits = limits; }

    internal QueryPolicyViolationKind? Evaluate(AbstractExpression root, CompiledQueryPolicy policy)
    {
        var pending = new Stack<(AbstractExpression, int)>(); pending.Push((root, 1)); int count = 0;
        while (pending.Count != 0)
        {
            var (node, depth) = pending.Pop();
            if (++count > _limits.MaxNodes || depth > _limits.MaxDepth ||
                node is Literal l && l.Value is string text && text.Length > _limits.MaxStringLength) return QueryPolicyViolationKind.LimitExceeded;
            foreach (var child in Children(node)) pending.Push((child, depth + 1));
        }
        foreach (var node in PostOrder(root))
            if (policy.Filter.Deny.Any(d => Match(node, d.Root))) return QueryPolicyViolationKind.DenyRuleMatched;
        if (policy.Filter.DefaultAllow) return DefaultOk(root) ? null : QueryPolicyViolationKind.LimitExceeded;
        return policy.Filter.Start != null && Match(root, policy.Filter.Start) ? null : QueryPolicyViolationKind.NotAllowed;
    }

    private bool Match(AbstractExpression node, BoundPattern p)
    {
        if ((TypeKinds.Of(node.ReturnType) & p.Types) == 0 || !_active.Add((node, p))) return false;
        try
        {
            switch (p.Kind)
            {
                case PatternKind.Any: return p.Deny || DefaultOk(node);
                case PatternKind.Hole: return node is Literal;
                case PatternKind.Constant: return node is Literal literal && Equals(literal.Value, p.Value);
                case PatternKind.Field:
                    return node is Field field && Same(field.Name, p.Name) && Same(field.Scope, p.Scope) ||
                        node is CollectionRef collection && Same(collection.Name, p.Name) && Same(collection.Scope, p.Scope);
                case PatternKind.Reference:
                case PatternKind.Union: return p.Children.Any(child => Match(node, child));
                case PatternKind.Contains: return Match(node, p.Children[0]) || Children(node).Any(child => Match(child, p));
                case PatternKind.Call:
                    if (node.GetType() != p.Function!.NodeType) return false;
                    var args = Children(node);
                    if (p.Shape == ArgumentShape.Exists)
                    {
                        if (!p.Deny && args.Count > Maximum(p.Function)) return false;
                        return Enumerable.Range(0, args.Count).Any(i => Match(args[i], p.Children[0]) &&
                            (p.Deny || args.Where((_, j) => j != i).All(DefaultOk)));
                    }
                    int fixedCount = p.Children.Count;
                    if (args.Count < fixedCount) return false;
                    if (p.Shape == ArgumentShape.Fixed && args.Count != fixedCount) return false;
                    if (p.Shape == ArgumentShape.Tail && (args.Count - fixedCount < p.Minimum || args.Count - fixedCount > p.Maximum)) return false;
                    for (int i = 0; i < args.Count; i++)
                        if (!Match(args[i], i < fixedCount ? p.Children[i] : p.TailPositions[Math.Min(i - fixedCount, p.TailPositions.Count - 1)])) return false;
                    return true;
                default: return false;
            }
        }
        finally { _active.Remove((node, p)); }
    }
    private int Maximum(FunctionInfo function) => function.CountKind == DefaultCountKind.Args ? _limits.MaxArgs :
        function.CountKind == DefaultCountKind.InItems ? _limits.MaxInItems + 1 : function.Maximum;
    private bool DefaultOk(AbstractExpression node) =>
        (!FunctionCatalog.ByType.TryGetValue(node.GetType(), out var f) || Children(node).Count <= Maximum(f)) && Children(node).All(DefaultOk);
    private static bool Same(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private static IReadOnlyList<AbstractExpression> Children(AbstractExpression node) =>
        node is AbstractFunction f ? f.Arguments : Array.Empty<AbstractExpression>();
    private static IEnumerable<AbstractExpression> PostOrder(AbstractExpression node)
    {
        foreach (var child in Children(node)) foreach (var descendant in PostOrder(child)) yield return descendant;
        yield return node;
    }
}
