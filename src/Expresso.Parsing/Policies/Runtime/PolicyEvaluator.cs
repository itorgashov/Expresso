using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Policies;
using Expresso.Parsing.Policies.Binding;
using Expresso.Parsing.Policies.Catalog;
using Expresso.Parsing.Policies.Syntax;

namespace Expresso.Parsing.Policies.Runtime;

internal sealed class NodeFacts
{
    internal NodeFacts(int count) { Bits = new(count); }
    internal RuleBits Bits { get; }
    internal bool DefaultOk { get; set; } = true;
    internal string? LimitName { get; set; }
    internal string? LimitPath { get; set; }
}

internal sealed class EvalContext
{
    private readonly AbstractExpression _node;
    private readonly EvalContext? _parent;
    private string? _path;
    internal EvalContext(CompiledQueryPolicy policy, QueryPolicyTarget target, AbstractExpression node, EvalContext? parent = null)
    {
        Policy = policy;
        Target = target;
        _node = node;
        _parent = parent;
    }
    internal CompiledQueryPolicy Policy { get; }
    internal QueryPolicyTarget Target { get; }
    internal string Path => _path ??= _parent == null ? FailureLocator.Name(_node) :
        _parent.Path + " > " + FailureLocator.Name(_node);
    internal PatternTable Table => Target == QueryPolicyTarget.Filter ? Policy.Filter : Policy.Sort;
}

internal sealed partial class PolicyEvaluator : IExpressoVisitor<EvalContext, NodeFacts>
{
    internal Dictionary<AbstractExpression, NodeFacts> Facts { get; } = new(ReferenceComparer.Instance);

    private NodeFacts Evaluate(AbstractExpression node, EvalContext context)
    {
        var table = context.Table;
        var children = node is AbstractFunction f ? f.Arguments.Select(child => child.Accept(this,
            new EvalContext(context.Policy, context.Target, child, context))).ToArray() : Array.Empty<NodeFacts>();
        var facts = new NodeFacts(table.Nodes.Length);
        var failedChild = children.FirstOrDefault(c => !c.DefaultOk);
        if (failedChild != null)
        { facts.DefaultOk = false; facts.LimitName = failedChild.LimitName; facts.LimitPath = failedChild.LimitPath; }
        if (FunctionCatalog.ByType.TryGetValue(node.GetType(), out var info) && children.Length > info.DefaultMaximum(context.Policy.Limits))
        {
            facts.DefaultOk = false;
            facts.LimitName = info.CountKind == DefaultCountKind.InItems ? "MaxInItems" : "MaxArgs";
            facts.LimitPath = context.Path;
        }
        var queue = new Queue<int>();
        TypeKind kind = TypeKinds.Of(node.ReturnType);
        void Add(BoundPattern pattern)
        {
            if ((pattern.Types & kind) != 0 && facts.Bits.Add(pattern.Id)) queue.Enqueue(pattern.Id);
        }
        foreach (var pattern in table.Any)
            if (pattern.Deny || facts.DefaultOk) Add(pattern);
        if (node is Literal literal)
        {
            foreach (var pattern in table.Holes) Add(pattern);
            if (table.Constants.TryGetValue((kind, literal.Value), out var constants))
                foreach (var pattern in constants) Add(pattern);
        }
        string? fieldKey = node switch
        {
            Field field => PatternTable.FieldKey(field.Scope, field.Name),
            CollectionRef collection => PatternTable.FieldKey(collection.Scope, collection.Name),
            _ => null
        };
        if (fieldKey != null && table.Fields.TryGetValue(fieldKey, out var fields))
            foreach (var pattern in fields) Add(pattern);
        if (table.Calls.TryGetValue(node.GetType(), out var calls))
            foreach (var call in calls)
                if (MatchesCall(call, children, context.Policy.Limits)) Add(call);
        if (table.ContainsById.Count != 0)
            foreach (var child in children)
                foreach (int matched in child.Bits.SetBits())
                    if (table.ContainsById.TryGetValue(matched, out var contains)) Add(contains);
        while (queue.Count != 0)
            foreach (int parent in table.Implications[queue.Dequeue()]) Add(table.Nodes[parent]);
        Facts[node] = facts;
        foreach (var deny in table.Deny)
            if (facts.Bits.Has(deny.Root.Id)) throw new QueryPolicyException(context.Target, QueryPolicyViolationKind.DenyRuleMatched,
                context.Path, deny.Text, errorDetail: context.Policy.ErrorDetail);
        return facts;
    }

    private static bool MatchesCall(BoundPattern pattern, NodeFacts[] children, QueryPolicyLimits limits)
    {
        int count = children.Length;
        if (pattern.Shape == ArgumentShape.Exists)
        {
            if (!pattern.Deny && count > pattern.Function!.DefaultMaximum(limits)) return false;
            for (int i = 0; i < count; i++)
                if (children[i].Bits.Has(pattern.Children[0].Id) &&
                    (pattern.Deny || children.Where((_, index) => index != i).All(c => c.DefaultOk))) return true;
            return false;
        }
        int fixedCount = pattern.Children.Count;
        if (count < fixedCount || pattern.Shape == ArgumentShape.Fixed && count != fixedCount) return false;
        for (int i = 0; i < fixedCount; i++) if (!children[i].Bits.Has(pattern.Children[i].Id)) return false;
        if (pattern.Shape == ArgumentShape.Fixed) return true;
        int repeated = count - fixedCount;
        if (repeated < pattern.Minimum || repeated > pattern.Maximum) return false;
        for (int i = fixedCount; i < count; i++) if (!children[i].Bits.Has(pattern.TailAt(i - fixedCount).Id)) return false;
        return true;
    }

    private sealed class ReferenceComparer : IEqualityComparer<AbstractExpression>
    {
        internal static readonly ReferenceComparer Instance = new();
        public bool Equals(AbstractExpression? x, AbstractExpression? y) => ReferenceEquals(x, y);
        public int GetHashCode(AbstractExpression obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
