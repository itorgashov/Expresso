using Expresso.Rendering.Linq;

namespace Expresso.Rendering.TestCases
{
    /// <summary>LINQ mapping for the shared widget catalog (same field names as the SQL <c>WidgetMapping</c>).</summary>
    public static class WidgetLinqMapping
    {
        public static LinqQueryMapping<WidgetTagMeta> TagMeta() =>
            new LinqQueryMapping<WidgetTagMeta>()
                .Field("kind", m => m.Kind)
                .Field("value", m => m.Value);

        public static LinqQueryMapping<WidgetTag> Tags() =>
            new LinqQueryMapping<WidgetTag>()
                .Field("label", t => t.Label)
                .Field("score", t => t.Score)
                .Collection("tag_meta", t => t.TagMeta, TagMeta());

        public static LinqQueryMapping<Widget> Create() =>
            new LinqQueryMapping<Widget>()
                .Field("name", w => w.Name)
                .Field("age", w => w.Age)
                .Field("amount", w => w.Amount)
                .Field("active", w => w.Active)
                .Field("created", w => w.Created)
                .Field("externalid", w => w.ExternalId)
                .Field("opens", w => w.Opens)
                .Field("notes", w => w.Notes)
                .Field("code", w => w.Code)
                .Collection("tags", w => w.Tags, Tags());
    }
}
