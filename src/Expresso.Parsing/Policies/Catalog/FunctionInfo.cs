using Expresso.Core.Policies;

namespace Expresso.Parsing.Policies.Catalog;

internal enum DefaultCountKind { None, Args, InItems }

internal sealed class FunctionSignature
{
    internal FunctionSignature(TypeKind result, TypeKind[] arguments)
    {
        Result = result;
        Arguments = arguments;
    }
    internal TypeKind Result { get; }
    internal TypeKind[] Arguments { get; }
    internal TypeKind At(int position) => Arguments[Math.Min(position, Arguments.Length - 1)];
}

internal sealed class FunctionInfo
{
    internal FunctionInfo(Type nodeType, string names, string category, int minimum, int maximum,
        TypeKind result, TypeKind[] arguments, int resultArgument = -1, bool sameType = false,
        DefaultCountKind countKind = DefaultCountKind.None)
    {
        NodeType = nodeType;
        Names = names.Split('|');
        Category = category;
        Minimum = minimum;
        Maximum = maximum;
        CountKind = countKind;
        var signatures = new List<FunctionSignature>();
        if (sameType)
            foreach (var kind in TypeKinds.Each(arguments[0])) signatures.Add(new(result, new[] { kind }));
        else if (resultArgument >= 0)
            foreach (var kind in TypeKinds.Each(arguments[resultArgument]))
            {
                var parameters = (TypeKind[])arguments.Clone(); parameters[resultArgument] = kind;
                var returnKind = names == "sum" && kind != TypeKind.Double ? TypeKind.Int : kind;
                signatures.Add(new(returnKind, parameters));
            }
        else signatures.Add(new(result, arguments));
        Signatures = signatures.ToArray();
    }
    internal Type NodeType { get; }
    internal string[] Names { get; }
    internal string Category { get; }
    internal int Minimum { get; }
    internal int Maximum { get; }
    internal DefaultCountKind CountKind { get; }
    internal FunctionSignature[] Signatures { get; }
    internal bool IsCollection => Category.StartsWith("collection-", StringComparison.Ordinal);
    internal bool IsQuantifier => Category == "collection-quantifier";
    internal int DefaultMaximum(QueryPolicyLimits limits) => CountKind switch
    {
        DefaultCountKind.Args => limits.MaxArgs,
        DefaultCountKind.InItems => limits.MaxInItems == int.MaxValue ? int.MaxValue : limits.MaxInItems + 1,
        _ => Maximum
    };
}
