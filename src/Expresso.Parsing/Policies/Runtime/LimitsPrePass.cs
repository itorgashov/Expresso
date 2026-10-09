using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Policies;

namespace Expresso.Parsing.Policies.Runtime;

internal static class LimitsPrePass
{
    private sealed class ParentNode
    {
        internal ParentNode(AbstractExpression node, ParentNode? parent)
        {
            Node = node;
            Parent = parent;
        }
        internal AbstractExpression Node { get; }
        internal ParentNode? Parent { get; }
    }

    internal static void Check(AbstractExpression root, CompiledQueryPolicy policy, QueryPolicyTarget target)
    {
        var stack = new Stack<(AbstractExpression Node, int Depth, ParentNode? Parent)>();
        stack.Push((root, 1, null));
        int nodes = 0;
        while (stack.Count != 0)
        {
            var (node, depth, parent) = stack.Pop();
            if (depth > policy.Limits.MaxDepth) Fail("MaxDepth", node, parent);
            if (++nodes > policy.Limits.MaxNodes) Fail("MaxNodes", node, parent);
            if (node is Literal literal && literal.Value is string text && text.Length > policy.Limits.MaxStringLength)
                Fail("MaxStringLength", node, parent);
            if (node is AbstractFunction function)
            {
                var link = new ParentNode(node, parent);
                for (int i = function.Arguments.Count - 1; i >= 0; i--)
                {
                    var child = function.Arguments[i];
                    stack.Push((child, depth + 1, link));
                }
            }
        }
        void Fail(string name, AbstractExpression node, ParentNode? parent)
        {
            var names = new Stack<string>();
            names.Push(FailureLocator.Name(node));
            for (var link = parent; link != null; link = link.Parent)
                names.Push(FailureLocator.Name(link.Node));
            throw new QueryPolicyException(target, QueryPolicyViolationKind.LimitExceeded,
                string.Join(" > ", names), limitName: name, errorDetail: policy.ErrorDetail);
        }
    }
}
