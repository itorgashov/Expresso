using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Filtering;
using Expresso.Core.Policies;
using Expresso.Parsing.Policies.Catalog;
using Expresso.Parsing.Policies.Syntax;

namespace Expresso.Parsing.Policies.Binding;

internal sealed partial class PolicyBinder
{
    private readonly Dictionary<string, PatternAst> _rules;
    private readonly QueryPolicyLimits _limits;
    private readonly LiteralFactory _literals;
    private readonly bool _deny, _sort;
    private readonly List<BoundPattern> _nodes = new();
    private readonly List<QueryPolicyDiagnostic> _errors = new();
    private readonly List<QueryPolicyDiagnostic> _warnings;
    private readonly HashSet<string> _used = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<BoundPattern>> _nested = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(PatternAst, string?, TypeKind, bool), BoundPattern> _cache = new();
    private readonly Queue<(PatternAst Ast, BindingScope Scope, TypeKind Types, bool SortPosition, BoundPattern Node)> _pending = new();
    private readonly Dictionary<(PatternAst, string?, string), List<BoundPattern>> _required = new();

    internal PolicyBinder(Dictionary<string, PatternAst> rules, QueryPolicyLimits limits, LiteralFactory literals,
        bool deny, bool sort, List<QueryPolicyDiagnostic> warnings)
    {
        _rules = rules;
        _limits = limits;
        _literals = literals;
        _deny = deny;
        _sort = sort;
        _warnings = warnings;
    }

    internal BoundPolicyRoot Bind(PatternAst ast, QueryModel model)
    {
        var root = Request(ast, new(model), _deny ? TypeKind.All : _sort ? TypeKind.SortValue : TypeKind.Boolean, _sort && !_deny);
        while (_pending.Count != 0)
        {
            var work = _pending.Dequeue();
            Build(work.Ast, work.Scope, work.Types, work.SortPosition, work.Node);
        }
        ComputeProductivity();
        foreach (var requirement in _required)
        {
            if (requirement.Value.Any(n => n.Productive)) continue;
            var (source, scope, code) = requirement.Key;
            _errors.Add(source.Token.Error(code, $"Pattern cannot match a valid expression in scope '{scope ?? "root"}'."));
        }
        if (!root.Productive && _errors.Count == 0)
            _errors.Add(ast.Token.Error("S4", "The start pattern has no viable expression for this target."));
        if (_errors.Count != 0) throw new QueryPolicyCompileException(_errors
            .GroupBy(d => (d.Code, d.Line, d.Column, d.Message)).Select(g => g.First()));
        return new(root, _nodes, _nested, _used);
    }

    private BoundPattern New(PatternKind kind, TypeKind types)
    {
        var node = new BoundPattern(kind, types, _deny);
        _nodes.Add(node);
        return node;
    }

    private BoundPattern Request(PatternAst ast, BindingScope scope, TypeKind types, bool sortPosition = false)
    {
        var key = (ast, scope.Path, types, sortPosition);
        if (_cache.TryGetValue(key, out var node)) return node;
        node = New(ast.Kind, types);
        _cache.Add(key, node);
        _pending.Enqueue((ast, scope, types, sortPosition, node));
        return node;
    }

    private void Require(PatternAst ast, BindingScope scope, BoundPattern node, string code = "S4")
    {
        var key = (ast, scope.Path, code);
        if (!_required.TryGetValue(key, out var nodes)) _required[key] = nodes = new();
        nodes.Add(node);
    }

    private void Build(PatternAst ast, BindingScope scope, TypeKind types, bool sortPosition, BoundPattern node)
    {
        switch (ast.Kind)
        {
            case PatternKind.Any: break;
            case PatternKind.Hole: node.Types &= TypeKind.Scalar; break;
            case PatternKind.Constant: BindConstant(ast, types, node); break;
            case PatternKind.Field:
                if (Resolve(ast.Name, scope, out var resolved, out var name, out var fieldType, out var collection))
                { node.Name = name; node.Scope = resolved.Path; node.Types &= collection == null ? TypeKinds.Of(fieldType!) : TypeKind.Collection; }
                else { node.Types = TypeKind.None; _errors.Add(ast.Token.Error("S1", $"Unknown field or collection '{ast.Name}' in scope '{scope.Path ?? "root"}'.")); }
                break;
            case PatternKind.Reference:
                _used.Add(ast.Name);
                if (!_rules.TryGetValue(ast.Name, out var definition))
                { node.Types = TypeKind.None; _errors.Add(ast.Token.Error("S5", $"Undefined rule '{ast.Name}'.")); }
                else node.Children.Add(Request(definition, scope, types, sortPosition));
                Require(ast, scope, node, "S6");
                break;
            case PatternKind.Union:
                foreach (var child in ast.Children)
                {
                    var bound = Request(child, scope, types, sortPosition); node.Children.Add(bound);
                    Require(child, scope, bound);
                }
                break;
            case PatternKind.Contains:
                // Carry the witness through every descendant type. Only the outer
                // reference is constrained by the containing function's signature.
                node.Kind = PatternKind.Reference;
                var witness = New(PatternKind.Contains, TypeKind.All);
                witness.Children.Add(Request(ast.Children[0], scope, TypeKind.All));
                node.Children.Add(witness);
                Require(ast, scope, node);
                break;
            case PatternKind.Call: BindCall(ast, scope, types, sortPosition, node); break;
            case PatternKind.SortFor:
                if (!sortPosition || !_sort || _deny || scope.Path != null)
                { node.Types = TypeKind.None; _errors.Add(ast.Token.Error("S7", "sortfor is allowed only in sort start alternatives.")); break; }
                if (!Resolve(ast.Name, scope, out var parent, out _, out _, out var childCollection) || childCollection == null)
                { node.Types = TypeKind.None; _errors.Add(ast.Token.Error("S1", $"Unknown collection path '{ast.Name}'.")); break; }
                var childScope = parent.Child(childCollection);
                var childPattern = Request(ast.Children[0], childScope, TypeKind.SortValue, true);
                node.Children.Add(childPattern);
                var path = childScope.Path!.Replace('.', '/');
                if (!_nested.TryGetValue(path, out var alternatives)) _nested[path] = alternatives = new();
                alternatives.Add(childPattern);
                Require(ast, scope, node);
                break;
        }
    }

    private void BindConstant(PatternAst ast, TypeKind types, BoundPattern node)
    {
        node.Kind = PatternKind.Union;
        var candidates = TypeKinds.Each(types & TypeKind.Scalar).ToArray();
        if (candidates.Length > 1)
        {
            var inferred = ast.Token.Kind == TokenKind.String ? TypeKind.String :
                ast.Name.IndexOfAny(new[] { '.', 'e', 'E' }) >= 0 ? TypeKind.Double : TypeKind.Int;
            candidates = candidates.Where(t => t == inferred).ToArray();
        }
        foreach (var kind in candidates)
        {
            try
            {
                var literal = (Literal)_literals.CreateLiteral(ast.Name, TypeKinds.Types[kind]);
                var constant = New(PatternKind.Constant, kind); constant.Value = literal.Value;
                node.Children.Add(constant);
            }
            catch (ArgumentException) { /* This signature is not viable; other signatures may parse it. */ }
        }
    }

    private static bool Resolve(string path, BindingScope scope, out BindingScope resolved,
        out string name, out Type? type, out CollectionModel? collection)
    {
        var parts = path.Split('/'); resolved = scope; type = null; collection = null;
        name = parts[parts.Length - 1];
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (!resolved.Model.TryGetCollection(parts[i], out var c)) return false;
            resolved = resolved.Child(c);
        }
        if (resolved.Model.TryGetField(name, out var field)) { type = field; return true; }
        if (resolved.Model.TryGetCollection(name, out var found)) { collection = found; return true; }
        return false;
    }

    private void ComputeProductivity()
    {
        bool changed;
        do
        {
            changed = false;
            foreach (var node in _nodes)
            {
                if (node.Productive || node.Types == TypeKind.None) continue;
                bool productive = node.Kind switch
                {
                    PatternKind.Union or PatternKind.Reference => node.Children.Any(c => c.Productive),
                    PatternKind.Contains or PatternKind.SortFor => node.Children[0].Productive,
                    PatternKind.Call => node.Children.All(c => c.Productive) &&
                        (node.Minimum == 0 || node.TailPositions.Take(Math.Min(node.Minimum, node.TailPositions.Count)).All(c => c.Productive)),
                    _ => true
                };
                if (productive) { node.Productive = true; changed = true; }
            }
        } while (changed);
    }
}
