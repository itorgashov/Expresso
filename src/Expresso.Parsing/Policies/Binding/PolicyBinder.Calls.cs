using Expresso.Core.Policies;
using Expresso.Parsing.Policies.Catalog;
using Expresso.Parsing.Policies.Syntax;

namespace Expresso.Parsing.Policies.Binding;

internal sealed partial class PolicyBinder
{
    private void BindCall(PatternAst ast, BindingScope scope, TypeKind types, bool sortPosition, BoundPattern node)
    {
        node.Kind = PatternKind.Union;
        var seen = new HashSet<Type>();
        foreach (var callee in ast.Callees)
        {
            var functions = FunctionCatalog.Resolve(callee.Text).ToArray();
            if (functions.Length == 0)
            { _errors.Add(callee.Error("S2", $"Unknown function or category '{callee.Text}'.")); continue; }
            var member = New(PatternKind.Union, types); node.Children.Add(member);
            bool arityFits = false, boundsFit = false;
            foreach (var function in functions)
            {
                if (!seen.Add(function.NodeType) && ast.Callees.Count > 1)
                    _warnings.Add(callee.Error("W4", $"Duplicate set member '{callee.Text}'.").AsWarning());
                if (sortPosition && function.IsQuantifier) continue;
                if (!ArityFits(ast, function)) continue;
                arityFits = true;
                if (!NormalizeBounds(ast, function, out int minimum, out int maximum)) continue;
                boundsFit = true;
                foreach (var signature in function.Signatures.Where(s => (s.Result & types) != 0))
                {
                    if (ast.Shape != ArgumentShape.Exists && ast.Children.Count != 0 &&
                        (PossibleTypes(ast.Children[0], scope) & signature.At(0)) == 0) continue;
                    if (function.IsCollection && ast.Shape != ArgumentShape.Exists && ast.Children.Count != 0)
                        Request(ast.Children[0], scope, TypeKind.Collection);
                    var scopes = CollectionScopes(ast, scope, function).ToArray();
                    foreach (var (itemScope, collectionPattern) in scopes)
                    {
                        var call = New(PatternKind.Call, signature.Result & types);
                        call.Function = function; call.Shape = ast.Shape;
                        call.Minimum = minimum; call.Maximum = maximum;
                        if (ast.Shape == ArgumentShape.Exists)
                        {
                            var kinds = signature.Arguments.Aggregate(TypeKind.None, (a, b) => a | b);
                            call.Children.Add(Request(ast.Children[0], scope, kinds));
                        }
                        else
                        {
                            for (int i = 0; i < ast.Children.Count; i++)
                                call.Children.Add(i == 0 && collectionPattern != null ? collectionPattern :
                                    Request(ast.Children[i], i == 1 ? itemScope : scope, signature.At(i)));
                            if (ast.Shape == ArgumentShape.Tail)
                            {
                                var tail = ast.Tail ?? new PatternAst(PatternKind.Any, ast.Token);
                                int end = function.Maximum == int.MaxValue ? Math.Max(ast.Children.Count + 1, signature.Arguments.Length) : function.Maximum;
                                for (int i = ast.Children.Count; i < end; i++)
                                    call.TailPositions.Add(Request(tail, i == 1 ? itemScope : scope, signature.At(i)));
                                call.Tail = call.TailPositions.FirstOrDefault();
                            }
                        }
                        member.Children.Add(call);
                    }
                }
            }
            // Group signature probes by source member, so a lenient category can discard
            // non-viable functions while every explicit set member must still be viable.
            var memberAst = MemberAst(ast, callee);
            string code = !arityFits ? "S3" : !boundsFit ? "S10" : callee.Kind == TokenKind.Category ? "S9" : "S4";
            Require(memberAst, scope, member, code);
        }
        Require(ast, scope, node);
    }

    private readonly Dictionary<(PatternAst, PolicyToken), PatternAst> _memberSources = new();
    private PatternAst MemberAst(PatternAst call, PolicyToken callee)
    {
        if (!_memberSources.TryGetValue((call, callee), out var ast))
            _memberSources[(call, callee)] = ast = new(PatternKind.Call, callee);
        return ast;
    }

    private static bool ArityFits(PatternAst ast, FunctionInfo function) => ast.Shape switch
    {
        ArgumentShape.Fixed => ast.Children.Count >= function.Minimum && ast.Children.Count <= function.Maximum,
        ArgumentShape.Exists => function.Maximum >= 1,
        _ => ast.Children.Count <= function.Maximum
    };

    private bool NormalizeBounds(PatternAst ast, FunctionInfo function, out int minimum, out int maximum)
    {
        minimum = 0; maximum = 0;
        if (ast.Shape != ArgumentShape.Tail) return true;
        int fixedCount = ast.Children.Count;
        int defaultMaximum = _deny ? function.Maximum : function.DefaultMaximum(_limits);
        minimum = Math.Max(ast.Minimum ?? (ast.Tail == null ? 0 : 1), function.Minimum - fixedCount);
        maximum = Math.Min(ast.Maximum ?? (defaultMaximum - fixedCount), function.Maximum - fixedCount);
        if (ast.ExplicitBounds && (ast.Maximum == 0 && ast.Minimum == 0 || minimum > maximum)) return false;
        if (minimum > maximum) return false;
        if (ast.ExplicitBounds && (long)fixedCount + minimum + 1 > _limits.MaxNodes)
            _warnings.Add(new("W2", QueryPolicySeverity.Warning, ast.Token.Line, ast.Token.Column,
                "The repetition's minimum tree size exceeds MaxNodes."));
        return true;
    }

    private IEnumerable<(BindingScope Scope, BoundPattern? Collection)> CollectionScopes(PatternAst call, BindingScope scope, FunctionInfo function)
    {
        if (!function.IsCollection || call.Shape == ArgumentShape.Exists || call.Children.Count == 0)
        { yield return (scope, null); yield break; }
        bool found = false;
        foreach (var candidate in LeafAlternatives(call.Children[0]))
        {
            if (candidate.Kind != PatternKind.Field ||
                !Resolve(candidate.Name, scope, out var parent, out var name, out _, out var collection) || collection == null) continue;
            found = true;
            var reference = New(PatternKind.Field, TypeKind.Collection);
            reference.Name = name; reference.Scope = parent.Path;
            yield return (parent.Child(collection), reference);
        }
        if (!found) yield return (scope, null);
    }

    private TypeKind PossibleTypes(PatternAst ast, BindingScope scope)
    {
        TypeKind result = TypeKind.None;
        foreach (var leaf in LeafAlternatives(ast))
        {
            if (leaf.Kind == PatternKind.Field)
            {
                if (Resolve(leaf.Name, scope, out _, out _, out var type, out var collection))
                    result |= collection != null ? TypeKind.Collection : TypeKinds.Of(type!);
                else return TypeKind.All; // Preserve the diagnostic from normal binding.
            }
            else if (leaf.Kind == PatternKind.Call)
                result |= leaf.Callees.SelectMany(c => FunctionCatalog.Resolve(c.Text))
                    .SelectMany(f => f.Signatures).Aggregate(TypeKind.None, (a, s) => a | s.Result);
            else result |= TypeKind.All;
        }
        return result;
    }

    private IEnumerable<PatternAst> LeafAlternatives(PatternAst ast)
    {
        var pending = new Stack<PatternAst>(); pending.Push(ast);
        var visited = new HashSet<PatternAst>();
        while (pending.Count != 0)
        {
            var current = pending.Pop(); if (!visited.Add(current)) continue;
            if (current.Kind == PatternKind.Union) foreach (var child in current.Children) pending.Push(child);
            else if (current.Kind == PatternKind.Reference && _rules.TryGetValue(current.Name, out var rule))
            { _used.Add(current.Name); pending.Push(rule); }
            else yield return current;
        }
    }
}

internal static class DiagnosticExtensions
{
    internal static QueryPolicyDiagnostic AsWarning(this QueryPolicyDiagnostic diagnostic) => new(
        diagnostic.Code, QueryPolicySeverity.Warning, diagnostic.Line, diagnostic.Column, diagnostic.Message);
}
