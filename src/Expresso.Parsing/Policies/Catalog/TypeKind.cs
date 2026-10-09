using Expresso.Core.CriteriaExpressions;

namespace Expresso.Parsing.Policies.Catalog;

[Flags]
internal enum TypeKind
{
    None = 0, Byte = 1, Int = 2, Double = 4, String = 8, Boolean = 16,
    DateTime = 32, Guid = 64, TimeSpan = 128, DateOnly = 256, TimeOnly = 512, Collection = 1024, Other = 2048,
    Numeric = Byte | Int | Double,
#if NET6_0_OR_GREATER
    Calendar = DateTime | DateOnly, Time = DateTime | TimeSpan | TimeOnly,
    Scalar = Numeric | String | Boolean | DateTime | Guid | TimeSpan | DateOnly | TimeOnly,
#else
    Calendar = DateTime, Time = DateTime | TimeSpan,
    Scalar = Numeric | String | Boolean | DateTime | Guid | TimeSpan,
#endif
    Ordered = Numeric | Calendar | Time, SortValue = Scalar | Other, All = SortValue | Collection
}

internal static class TypeKinds
{
    internal static readonly IReadOnlyDictionary<TypeKind, Type> Types = new Dictionary<TypeKind, Type>
    {
        [TypeKind.Byte] = typeof(byte), [TypeKind.Int] = typeof(int), [TypeKind.Double] = typeof(double),
        [TypeKind.String] = typeof(string), [TypeKind.Boolean] = typeof(bool), [TypeKind.DateTime] = typeof(DateTime),
        [TypeKind.Guid] = typeof(Guid), [TypeKind.TimeSpan] = typeof(TimeSpan), [TypeKind.Collection] = typeof(CollectionRef),
#if NET6_0_OR_GREATER
        [TypeKind.DateOnly] = typeof(DateOnly), [TypeKind.TimeOnly] = typeof(TimeOnly),
#endif
    };
    internal static TypeKind Of(Type type)
    {
        var known = Types.FirstOrDefault(p => p.Value == type).Key;
        return known == TypeKind.None ? TypeKind.Other : known;
    }
    internal static IEnumerable<TypeKind> Each(TypeKind kinds) => Types.Keys.Where(t => (t & kinds) != 0)
        .Concat((kinds & TypeKind.Other) != 0 ? new[] { TypeKind.Other } : Array.Empty<TypeKind>());
}
