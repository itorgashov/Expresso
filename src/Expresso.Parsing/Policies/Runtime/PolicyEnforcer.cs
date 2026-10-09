using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Policies;
using Expresso.Core.Sorting;
using Expresso.Parsing.Policies.Binding;

namespace Expresso.Parsing.Policies.Runtime;

internal static class PolicyEnforcer
{
    internal static void EnforceFilter(QueryModel model, AbstractExpression expression)
    {
        var policy = GetPolicy(model, QueryPolicyTarget.Filter);
        Check(expression, policy, QueryPolicyTarget.Filter, "");
    }
    internal static void EnforceSort(QueryModel model, SortDirective directive)
    {
        var policy = GetPolicy(model, QueryPolicyTarget.Sort);
        if (directive.TotalSortKeyCount() > policy.Limits.MaxSortKeys)
            throw new QueryPolicyException(QueryPolicyTarget.Sort, QueryPolicyViolationKind.LimitExceeded,
                limitName: "MaxSortKeys", errorDetail: policy.ErrorDetail);
        var pending = new Stack<(SortDirective Directive, string Path)>(); pending.Push((directive, ""));
        while (pending.Count != 0)
        {
            var (current, path) = pending.Pop();
            foreach (var item in current.Items) Check(item.Expression, policy, QueryPolicyTarget.Sort, path);
            foreach (var nested in current.Nested.Reverse())
                pending.Push((nested.Directive, path.Length == 0 ? nested.Name : path + "/" + nested.Name));
        }
    }

    private static CompiledQueryPolicy GetPolicy(QueryModel model, QueryPolicyTarget target)
    {
        if (model.Policy is not CompiledQueryPolicy policy)
            throw new InvalidOperationException("Only QueryPolicyCompiler policies can be enforced.");
        var expected = target == QueryPolicyTarget.Filter ? policy.FilterFingerprint : policy.SortFingerprint;
        if (!string.Equals(model.CatalogFingerprint, expected, StringComparison.Ordinal))
            throw new InvalidOperationException("The policy was compiled against a different query catalog.");
        return policy;
    }

    private static void Check(AbstractExpression expression, CompiledQueryPolicy policy, QueryPolicyTarget target, string path)
    {
        LimitsPrePass.Check(expression, policy, target);
        var evaluator = new PolicyEvaluator();
        var facts = expression.Accept(evaluator, new EvalContext(policy, target, expression));
        var table = target == QueryPolicyTarget.Filter ? policy.Filter : policy.Sort;
        if (table.DefaultAllow)
        {
            if (!facts.DefaultOk) throw new QueryPolicyException(target, QueryPolicyViolationKind.LimitExceeded,
                facts.LimitPath, limitName: facts.LimitName, errorDetail: policy.ErrorDetail);
            return;
        }
        BoundPattern[] starts = path.Length == 0 ? table.Start == null ? Array.Empty<BoundPattern>() : new[] { table.Start } :
            table.Nested.TryGetValue(path, out var alternatives) ? alternatives : Array.Empty<BoundPattern>();
        if (starts.Any(p => facts.Bits.Has(p.Id))) return;
        throw new QueryPolicyException(target, QueryPolicyViolationKind.NotAllowed,
            FailureLocator.Locate(expression, starts, evaluator), errorDetail: policy.ErrorDetail);
    }
}
