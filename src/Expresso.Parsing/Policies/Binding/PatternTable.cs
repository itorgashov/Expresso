using Expresso.Parsing.Policies.Catalog;
using Expresso.Parsing.Policies.Syntax;

namespace Expresso.Parsing.Policies.Binding;

internal sealed class PatternTable
{
    internal PatternTable(BoundPolicyRoot? start, IReadOnlyList<(BoundPolicyRoot Root, string Text)> deny, bool defaultAllow)
    {
        var nodes = new List<BoundPattern>();
        if (start != null) nodes.AddRange(start.Nodes);
        foreach (var rule in deny) nodes.AddRange(rule.Root.Nodes);
        Nodes = nodes.ToArray();
        for (int i = 0; i < Nodes.Length; i++) Nodes[i].Id = i;
        Start = start?.Root;
        DefaultAllow = defaultAllow;
        Deny = deny.Select(d => (d.Root.Root, d.Text)).ToArray();
        Nested = start?.Nested.ToDictionary(p => p.Key, p => p.Value.ToArray(), StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, BoundPattern[]>(StringComparer.OrdinalIgnoreCase);
        Calls = Nodes.Where(n => n.Kind == PatternKind.Call && n.Productive)
            .GroupBy(n => n.Function!.NodeType).ToDictionary(g => g.Key, g => g.ToArray());
        var leaves = Nodes.Where(n => n.Productive && n.Kind is PatternKind.Any or PatternKind.Hole or PatternKind.Field or PatternKind.Constant).ToArray();
        Any = leaves.Where(n => n.Kind == PatternKind.Any).ToArray();
        Holes = leaves.Where(n => n.Kind == PatternKind.Hole).ToArray();
        Fields = leaves.Where(n => n.Kind == PatternKind.Field)
            .GroupBy(n => FieldKey(n.Scope, n.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.OrdinalIgnoreCase);
        Constants = leaves.Where(n => n.Kind == PatternKind.Constant)
            .GroupBy(n => (n.Types, n.Value)).ToDictionary(g => g.Key, g => g.ToArray());
        ContainsById = Nodes.Where(n => n.Productive && n.Kind == PatternKind.Contains)
            .ToDictionary(n => n.Id);
        var parents = Enumerable.Range(0, Nodes.Length).Select(_ => new List<int>()).ToArray();
        foreach (var node in Nodes.Where(n => n.Productive && n.Kind is PatternKind.Union or PatternKind.Reference or PatternKind.Contains))
            foreach (var child in node.Children) parents[child.Id].Add(node.Id);
        Implications = parents.Select(p => p.Distinct().ToArray()).ToArray();
    }
    internal BoundPattern[] Nodes { get; }
    internal BoundPattern? Start { get; }
    internal bool DefaultAllow { get; }
    internal (BoundPattern Root, string Text)[] Deny { get; }
    internal IReadOnlyDictionary<string, BoundPattern[]> Nested { get; }
    internal IReadOnlyDictionary<Type, BoundPattern[]> Calls { get; }
    internal BoundPattern[] Any { get; }
    internal BoundPattern[] Holes { get; }
    internal IReadOnlyDictionary<string, BoundPattern[]> Fields { get; }
    internal IReadOnlyDictionary<(TypeKind, object?), BoundPattern[]> Constants { get; }
    internal IReadOnlyDictionary<int, BoundPattern> ContainsById { get; }
    internal int[][] Implications { get; }
    internal static string FieldKey(string? scope, string name) => (scope ?? "") + "\0" + name;
}
