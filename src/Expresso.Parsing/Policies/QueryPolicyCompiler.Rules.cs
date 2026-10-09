using Expresso.Core.Policies;
using Expresso.Parsing.Policies.Catalog;
using Expresso.Parsing.Policies.Syntax;

namespace Expresso.Parsing.Policies;

public static partial class QueryPolicyCompiler
{
    private static Dictionary<string, PatternAst> CollectRules(IReadOnlyList<StatementAst> statements, List<QueryPolicyDiagnostic> errors)
    {
        var rules = new Dictionary<string, PatternAst>(StringComparer.OrdinalIgnoreCase);
        foreach (var statement in statements.Where(s => s.Kind == StatementKind.Rule))
        {
            bool exists = rules.TryGetValue(statement.Name, out var previous);
            if (statement.Extend != exists)
            {
                errors.Add(statement.Token.Error("S5", statement.Extend
                    ? $"Rule '{statement.Name}' must be defined before extending it."
                    : $"Rule '{statement.Name}' is already defined."));
                continue;
            }
            if (!exists) rules[statement.Name] = statement.Pattern!;
            else
            {
                var union = new PatternAst(PatternKind.Union, previous!.Token);
                // Flatten extensions to avoid a deep chain from a long configuration.
                if (previous.Kind == PatternKind.Union) union.Children.AddRange(previous.Children);
                else union.Children.Add(previous);
                union.Children.Add(statement.Pattern!); rules[statement.Name] = union;
            }
        }
        return rules;
    }

    private static void CollectDefaults(IReadOnlyList<StatementAst> statements,
        Dictionary<string, PatternAst> rules, IReadOnlyList<CompilationSide> sides, List<QueryPolicyDiagnostic> errors)
    {
        var defaults = new Dictionary<string, StatementAst>();
        foreach (var statement in statements.Where(s => s.Kind == StatementKind.Default))
        {
            string target = statement.Target ?? "both";
            if (defaults.ContainsKey(target)) errors.Add(statement.Token.Error("S8", $"Duplicate default for {target}."));
            else defaults.Add(target, statement);
        }
        foreach (var side in sides)
        {
            if (defaults.TryGetValue(side.Name, out var statement) || defaults.TryGetValue("both", out statement))
            {
                side.DefaultAllow = statement.Allow;
                if (statement.Allow && rules.ContainsKey(side.Name)) errors.Add(statement.Token.Error("S8", $"default {side.Name}: allow cannot coexist with a {side.Name} start rule."));
            }
        }
    }

    private static void ValidateRuleGraph(Dictionary<string, PatternAst> rules, List<QueryPolicyDiagnostic> errors)
    {
        // This structural check also covers unused rules, which have no catalog scope
        // instance. A least fixed point recognizes finite derivations without recursion.
        var nodes = new HashSet<PatternAst>(); var stack = new Stack<PatternAst>(rules.Values);
        while (stack.Count != 0)
        {
            var node = stack.Pop(); if (!nodes.Add(node)) continue;
            foreach (var child in node.Children) stack.Push(child);
            if (node.Tail != null) stack.Push(node.Tail);
            if (node.Kind == PatternKind.Reference && !rules.ContainsKey(node.Name))
                errors.Add(node.Token.Error("S5", $"Undefined rule '{node.Name}'."));
            foreach (var callee in node.Callees)
                if (!FunctionCatalog.Resolve(callee.Text).Any())
                    errors.Add(callee.Error("S2", $"Unknown function or category '{callee.Text}'."));
        }
        var productive = new HashSet<PatternAst>(); bool changed;
        do
        {
            changed = false;
            foreach (var node in nodes)
            {
                if (productive.Contains(node)) continue;
                bool good = node.Kind switch
                {
                    PatternKind.Reference => rules.TryGetValue(node.Name, out var definition) && productive.Contains(definition),
                    PatternKind.Union => node.Children.Any(productive.Contains),
                    PatternKind.Call => node.Children.All(productive.Contains) &&
                        (node.Tail == null || node.Minimum == 0 || productive.Contains(node.Tail)),
                    PatternKind.Contains or PatternKind.SortFor => node.Children.All(productive.Contains),
                    _ => true
                };
                if (good) { productive.Add(node); changed = true; }
            }
        } while (changed);
        foreach (var rule in rules.Where(p => !productive.Contains(p.Value)))
            errors.Add(rule.Value.Token.Error("S6", $"Rule '{rule.Key}' has no finite derivation."));
    }
}
