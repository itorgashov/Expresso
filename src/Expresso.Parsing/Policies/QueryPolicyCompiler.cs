using Expresso.Core.Filtering;
using Expresso.Core.Policies;
using Expresso.Parsing.Policies.Binding;
using Expresso.Parsing.Policies.Catalog;
using Expresso.Parsing.Policies.Runtime;
using Expresso.Parsing.Policies.Syntax;

namespace Expresso.Parsing.Policies;

/// <summary>Compiles an endpoint's policy once against its filter and sort catalogs.</summary>
public static partial class QueryPolicyCompiler
{
    /// <summary>Validates rules, binds names and types, and returns policy-bearing models.</summary>
    /// <param name="definition">Policy source, limits, and error detail options.</param>
    /// <param name="filterModel">The endpoint's filter catalog.</param>
    /// <param name="sortModel">The endpoint's sort catalog.</param>
    /// <param name="literalOptions">The same literal options used by the request parsers.</param>
    /// <exception cref="QueryPolicyCompileException">The policy has lexical, syntax, or semantic errors.</exception>
    public static QueryPolicyModels Compile(QueryPolicyDefinition definition, QueryModel filterModel,
        QueryModel sortModel, LiteralParseOptions? literalOptions = null)
    {
        var errors = ValidateInputs(definition, filterModel, sortModel);
        if (errors.Count != 0) throw new QueryPolicyCompileException(errors);
        var limits = CopyLimits(definition.Limits);
        var statements = new PolicySyntaxParser(definition.Rules).Parse();
        var rules = CollectRules(statements, errors);
        var filter = new CompilationSide(QueryPolicyTarget.Filter, filterModel);
        var sort = new CompilationSide(QueryPolicyTarget.Sort, sortModel);
        var sides = new[] { filter, sort };
        CollectDefaults(statements, rules, sides, errors);
        var warnings = new List<QueryPolicyDiagnostic>();
        var literals = new LiteralFactory(literalOptions);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var side in sides)
        {
            if (rules.TryGetValue(side.Name, out var start))
                side.Start = Bind(start, side);
            else if (side.DefaultAllow)
                side.Start = Bind(new(PatternKind.Any, new(TokenKind.Punctuation, "*", 1, 1, 0)), side);
            else
                warnings.Add(new("W3", QueryPolicySeverity.Warning, 1, 1, $"No {side.Name} allow rule: every {side.Name} is rejected."));
        }
        foreach (var statement in statements.Where(s => s.Kind == StatementKind.Deny))
        {
            var failed = new List<QueryPolicyDiagnostic>();
            int successes = 0;
            foreach (var side in sides)
            {
                if (statement.Target != null && statement.Target != side.Name) continue;
                try
                {
                    var bound = new PolicyBinder(rules, limits, literals, true, side.Target == QueryPolicyTarget.Sort, warnings)
                        .Bind(statement.Pattern!, side.Model);
                    side.Deny.Add((bound, statement.Text));
                    used.UnionWith(bound.UsedRules);
                    successes++;
                }
                catch (QueryPolicyCompileException ex) { failed.AddRange(ex.Diagnostics); }
            }
            if (successes == 0) errors.AddRange(failed);
        }
        ValidateRuleGraph(rules, errors);
        foreach (var rule in rules.Where(p => p.Key.StartsWith("$", StringComparison.Ordinal) && !used.Contains(p.Key)))
            warnings.Add(new("W1", QueryPolicySeverity.Warning, rule.Value.Token.Line, rule.Value.Token.Column, $"Unused rule '{rule.Key}'."));
        if (errors.Count != 0) throw new QueryPolicyCompileException(errors
            .GroupBy(d => (d.Code, d.Line, d.Column, d.Message)).Select(g => g.First()));
        var filterTable = new PatternTable(filter.Start, filter.Deny, filter.DefaultAllow);
        var sortTable = new PatternTable(sort.Start, sort.Deny, sort.DefaultAllow);
        var policy = new CompiledQueryPolicy(filterTable, sortTable, limits, definition.ErrorDetail,
            filterModel.CatalogFingerprint, sortModel.CatalogFingerprint);
        return new(filterModel.WithPolicy(policy), sortModel.WithPolicy(policy), warnings
            .GroupBy(d => (d.Code, d.Line, d.Column, d.Message)).Select(g => g.First())
            .OrderBy(d => d.Line).ThenBy(d => d.Column));

        BoundPolicyRoot? Bind(PatternAst ast, CompilationSide side)
        {
            try
            {
                var bound = new PolicyBinder(rules, limits, literals, false, side.Target == QueryPolicyTarget.Sort, warnings)
                    .Bind(ast, side.Model);
                used.UnionWith(bound.UsedRules);
                return bound;
            }
            catch (QueryPolicyCompileException ex)
            {
                errors.AddRange(ex.Diagnostics);
                return null;
            }
        }
    }

    private sealed class CompilationSide
    {
        internal CompilationSide(QueryPolicyTarget target, QueryModel model)
        {
            Target = target;
            Model = model;
            Name = target switch
            {
                QueryPolicyTarget.Filter => "filter",
                QueryPolicyTarget.Sort => "sort",
                _ => throw new ArgumentOutOfRangeException(nameof(target))
            };
        }

        internal QueryPolicyTarget Target { get; }
        internal string Name { get; }
        internal QueryModel Model { get; }
        internal BoundPolicyRoot? Start { get; set; }
        internal bool DefaultAllow { get; set; }
        internal List<(BoundPolicyRoot, string)> Deny { get; } = new();
    }

    private static List<QueryPolicyDiagnostic> ValidateInputs(QueryPolicyDefinition definition, QueryModel filter, QueryModel sort)
    {
        var errors = new List<QueryPolicyDiagnostic>();
        void Error(string code, string text) => errors.Add(new(code, QueryPolicySeverity.Error, 1, 1, text));
        if (definition == null || definition.Rules == null) Error("S12", "Policy definition and rules must not be null.");
        if (filter == null || sort == null) Error("S12", "Both query models are required.");
        var limits = definition?.Limits;
        if (limits == null) Error("S11", "Limits must not be null.");
        else
        {
            foreach (var limit in new[] { (nameof(limits.MaxDepth), limits.MaxDepth), (nameof(limits.MaxNodes), limits.MaxNodes),
                (nameof(limits.MaxArgs), limits.MaxArgs), (nameof(limits.MaxInItems), limits.MaxInItems),
                (nameof(limits.MaxStringLength), limits.MaxStringLength), (nameof(limits.MaxSortKeys), limits.MaxSortKeys) })
                if (limit.Item2 < 1) Error("S11", limit.Item1 + " must be at least 1.");
            if (limits.MaxDepth > 100) Error("S11", "MaxDepth must be at most 100.");
        }
        if (definition != null && !Enum.IsDefined(typeof(QueryPolicyErrorDetail), definition.ErrorDetail)) Error("S11", "Unknown ErrorDetail.");
        return errors;
    }
    private static QueryPolicyLimits CopyLimits(QueryPolicyLimits value) => new()
    {
        MaxDepth = value.MaxDepth, MaxNodes = value.MaxNodes, MaxArgs = value.MaxArgs,
        MaxInItems = value.MaxInItems, MaxStringLength = value.MaxStringLength, MaxSortKeys = value.MaxSortKeys
    };
}
