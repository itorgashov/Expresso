using Expresso.Core.Filtering;
using Expresso.Parsing.Policies.Catalog;
using Expresso.Parsing.Policies.Syntax;

namespace Expresso.Parsing.Policies.Binding;

internal sealed class BoundPattern
{
    internal BoundPattern(PatternKind kind, TypeKind types, bool deny)
    {
        Kind = kind;
        Types = types;
        Deny = deny;
    }
    internal int Id { get; set; }
    internal PatternKind Kind { get; set; }
    internal TypeKind Types { get; set; }
    internal bool Deny { get; }
    internal bool Productive { get; set; }
    internal List<BoundPattern> Children { get; } = new();
    internal FunctionInfo? Function { get; set; }
    internal ArgumentShape Shape { get; set; }
    internal BoundPattern? Tail { get; set; }
    internal List<BoundPattern> TailPositions { get; } = new();
    internal BoundPattern TailAt(int index) => TailPositions[Math.Min(index, TailPositions.Count - 1)];
    internal int Minimum { get; set; }
    internal int Maximum { get; set; }
    internal string Name { get; set; } = "";
    internal string? Scope { get; set; }
    internal object? Value { get; set; }
}

internal sealed class BindingScope
{
    internal BindingScope(QueryModel model, string? path = null)
    {
        Model = model;
        Path = path;
    }
    internal QueryModel Model { get; }
    internal string? Path { get; }
    internal BindingScope Child(CollectionModel collection) => new(collection.Items,
        Path == null ? collection.Name : Path + "." + collection.Name);
}

internal sealed class BoundPolicyRoot
{
    internal BoundPolicyRoot(BoundPattern root, IReadOnlyList<BoundPattern> nodes,
        IReadOnlyDictionary<string, List<BoundPattern>> nested, ISet<string> usedRules)
    {
        Root = root;
        Nodes = nodes;
        Nested = nested;
        UsedRules = usedRules;
    }
    internal BoundPattern Root { get; }
    internal IReadOnlyList<BoundPattern> Nodes { get; }
    internal IReadOnlyDictionary<string, List<BoundPattern>> Nested { get; }
    internal ISet<string> UsedRules { get; }
}
