using Expresso.Rendering.Linq;
using System.Linq.Expressions;

namespace Expresso.Rendering.TestCases;

/// <summary>A base-typed scope with subtype-only collection/reference navigations.</summary>
public static class DerivedCastProjectionFixture
{
    public static List<CastNode> Data()
    {
        var derived = new CastSpecialNode { Id = 1, Name = "special", TargetId = 1 };
        var item = new CastItem { Id = 1, Name = "item", CommonId = 1, SpecialId = 1 };
        derived.Common.Add(item);
        derived.Special.Add(item);
        derived.Target = derived;
        return new List<CastNode> { derived, new CastNode { Id = 2, Name = "base" } };
    }
    public static LinqProjectionMapping<CastNode, CastNodeDto> Mapping(Expression<Func<CastNode, IEnumerable<CastItem>>>? navigation = null) =>
        new LinqProjectionMapping<CastNode, CastNodeDto>().Key("id", n => n.Id).Field("name", "name", n => n.Name, n => n.Name)
            .Collection("items", "items", navigation ?? (n => n.Common), n => n.Items,
                new LinqProjectionMapping<CastItem, CastItemDto>().Key("id", i => i.Id).Field("name", "name", i => i.Name, i => i.Name), new LinqQueryMapping<CastItem>());
}
public interface ICastNode
{
    List<CastItem> Common { get; set; }
}
public class CastNode : ICastNode
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<CastItem> Common { get; set; } = new();
}
public class CastSpecialNode : CastNode
{
    public List<CastItem> Special { get; set; } = new();
    public int? TargetId { get; set; }
    public CastNode? Target { get; set; }
}
public class CastItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int CommonId { get; set; }
    public int? SpecialId { get; set; }
}
public class CastNodeDto { public string Name { get; set; } = ""; public List<CastItemDto> Items { get; set; } = new(); }
public class CastItemDto { public string Name { get; set; } = ""; }
