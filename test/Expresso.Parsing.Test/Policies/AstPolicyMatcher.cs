using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Parsing.Policies.Catalog;
using Expresso.Parsing.Policies.Syntax;

namespace Expresso.Parsing.Test.Policies;

// A small policy-language oracle over syntax and parsed IR. It never reads bound
// patterns, type masks, bitsets, or the compiler's pattern table.
internal sealed class AstPolicyMatcher
{
    private readonly QueryModel _model;
    private readonly Dictionary<string, PatternAst> _rules;
    private readonly PatternAst? _filter;
    private readonly PatternAst? _sort;
    private readonly (PatternAst Pattern, string? Target)[] _denies;
    private readonly LiteralFactory _literals = new();

    internal AstPolicyMatcher(string source, QueryModel model)
    {
        _model = model;
        var statements = new PolicySyntaxParser(source).Parse();
        _rules = statements.Where(s => s.Kind == StatementKind.Rule && s.Name.StartsWith("$"))
            .ToDictionary(s => s.Name, s => s.Pattern!, StringComparer.OrdinalIgnoreCase);
        _filter = statements.FirstOrDefault(s => s.Kind == StatementKind.Rule && s.Name == "filter")?.Pattern;
        _sort = statements.FirstOrDefault(s => s.Kind == StatementKind.Rule && s.Name == "sort")?.Pattern;
        _denies = statements.Where(s => s.Kind == StatementKind.Deny)
            .Select(s => (s.Pattern!, s.Target)).ToArray();
    }

    internal bool AllowsFilter(AbstractExpression root) =>
        !Denied(root, "filter") && _filter != null && Match(root, _filter, _model, null);

    internal bool AllowsSort(SortDirective directive)
    {
        if (_sort == null) return false;
        var pending = new Stack<(SortDirective Directive, string Path)>();
        pending.Push((directive, ""));
        while (pending.Count != 0)
        {
            var (current, path) = pending.Pop();
            QueryModel scope = ScopeAt(path);
            foreach (var item in current.Items)
            {
                if (Denied(item.Expression, "sort") || !MatchSort(item.Expression, _sort, scope, path))
                    return false;
            }
            foreach (var nested in current.Nested)
                pending.Push((nested.Directive, path.Length == 0 ? nested.Name : path + "/" + nested.Name));
        }
        return true;
    }

    private bool Denied(AbstractExpression root, string target) => Descendants(root).Any(node =>
        _denies.Any(deny => (deny.Target == null || deny.Target == target) && Match(node, deny.Pattern, _model, null)));

    private bool MatchSort(AbstractExpression node, PatternAst pattern, QueryModel scope, string path)
    {
        if (pattern.Kind == PatternKind.Union) return pattern.Children.Any(p => MatchSort(node, p, scope, path));
        if (pattern.Kind == PatternKind.Reference) return MatchSort(node, _rules[pattern.Name], scope, path);
        if (pattern.Kind == PatternKind.SortFor)
            return path.Equals(pattern.Name, StringComparison.OrdinalIgnoreCase) && Match(node, pattern.Children[0], scope, path.Replace('/', '.'));
        return path.Length == 0 && Match(node, pattern, scope, null);
    }

    private bool Match(AbstractExpression node, PatternAst pattern, QueryModel scope, string? scopePath)
    {
        switch (pattern.Kind)
        {
            case PatternKind.Any: return true;
            case PatternKind.Hole: return node is Literal;
            case PatternKind.Constant:
                if (node is not Literal literal) return false;
                try { return Equals(((Literal)_literals.CreateLiteral(pattern.Name, node.ReturnType)).Value, literal.Value); }
                catch (ArgumentException) { return false; }
            case PatternKind.Field: return MatchField(node, pattern.Name, scope, scopePath);
            case PatternKind.Union: return pattern.Children.Any(child => Match(node, child, scope, scopePath));
            case PatternKind.Reference: return Match(node, _rules[pattern.Name], scope, scopePath);
            case PatternKind.Contains: return Match(node, pattern.Children[0], scope, scopePath) ||
                Children(node).Any(child => Match(child, pattern, scope, scopePath));
            case PatternKind.Call: return MatchCall(node, pattern, scope, scopePath);
            default: return false;
        }
    }

    private bool MatchField(AbstractExpression node, string path, QueryModel scope, string? scopePath)
    {
        var parts = path.Split('/');
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (!scope.TryGetCollection(parts[i], out var collection)) return false;
            scope = collection.Items;
            scopePath = scopePath == null ? parts[i] : scopePath + "." + parts[i];
        }
        string name = parts[parts.Length - 1];
        if (node is Field field)
            return scope.TryGetField(name, out var type) && type == field.ReturnType &&
                Same(name, field.Name) && Same(scopePath, field.Scope);
        return node is CollectionRef reference && scope.TryGetCollection(name, out _) &&
            Same(name, reference.Name) && Same(scopePath, reference.Scope);
    }

    private bool MatchCall(AbstractExpression node, PatternAst pattern, QueryModel scope, string? scopePath)
    {
        if (node is not AbstractFunction function ||
            !pattern.Callees.SelectMany(c => FunctionCatalog.Resolve(c.Text)).Any(f => f.NodeType == node.GetType())) return false;
        var args = function.Arguments;
        if (pattern.Shape == ArgumentShape.Exists)
            return args.Any(arg => Match(arg, pattern.Children[0], scope, scopePath));
        int fixedCount = pattern.Children.Count;
        if (args.Count < fixedCount || pattern.Shape == ArgumentShape.Fixed && args.Count != fixedCount) return false;
        if (pattern.Shape == ArgumentShape.Tail)
        {
            int repeated = args.Count - fixedCount;
            if (repeated < (pattern.Minimum ?? (pattern.Tail == null ? 0 : 1)) ||
                repeated > (pattern.Maximum ?? int.MaxValue)) return false;
        }
        QueryModel childScope = scope;
        string? childPath = scopePath;
        if (args.Count > 0 && args[0] is CollectionRef collection && scope.TryGetCollection(collection.Name, out var model))
        {
            childScope = model.Items;
            childPath = scopePath == null ? collection.Name : scopePath + "." + collection.Name;
        }
        for (int i = 0; i < args.Count; i++)
        {
            var childPattern = i < fixedCount ? pattern.Children[i] : pattern.Tail;
            if (childPattern == null) continue;
            if (!Match(args[i], childPattern, i == 1 ? childScope : scope, i == 1 ? childPath : scopePath)) return false;
        }
        return true;
    }

    private QueryModel ScopeAt(string path)
    {
        QueryModel scope = _model;
        foreach (string name in path.Split('/').Where(part => part.Length != 0))
            scope = scope.TryGetCollection(name, out var collection) ? collection.Items : throw new InvalidOperationException(path);
        return scope;
    }

    private static bool Same(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private static IReadOnlyList<AbstractExpression> Children(AbstractExpression node) =>
        node is AbstractFunction function ? function.Arguments : Array.Empty<AbstractExpression>();
    private static IEnumerable<AbstractExpression> Descendants(AbstractExpression node)
    {
        yield return node;
        foreach (var child in Children(node))
            foreach (var descendant in Descendants(child)) yield return descendant;
    }
}
