using Expresso.Rendering;

namespace Expresso.Rendering.Integration.Test
{
    public static class WidgetMapping
    {
        public const string ParamPrefix = "p";

        public static SqlQueryMapping Create()
        {
            return new SqlQueryMapping(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["name"] = "name",
                    ["age"] = "age",
                    ["amount"] = "amount",
                    ["active"] = "active",
                    ["created"] = "created_at",
                    ["externalid"] = "external_id",
                    ["opens"] = "opens",
                    ["notes"] = "notes",
                    ["code"] = "code",
                },
                new[]
                {
                    new CollectionSqlMapping(
                        "tags",
                        "widget_tag",
                        "widget_tag.widget_id = widget.id",
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["label"] = "label",
                            ["score"] = "score",
                        },
                        new[]
                        {
                            new CollectionSqlMapping(
                                "tag_meta",
                                "widget_tag_meta",
                                "widget_tag_meta.tag_id = widget_tag.id",
                                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                {
                                    ["kind"] = "kind",
                                    ["value"] = "value",
                                }),
                        }),
                });
        }

        public static SqlQueryMapping CreateQuoted(string? parentAlias = null)
        {
            var parentId = parentAlias is null ? "\"widget\".\"id\"" : parentAlias + ".\"id\"";
            return new SqlQueryMapping(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["name"] = "name",
                    ["age"] = "age",
                    ["amount"] = "amount",
                    ["active"] = "active",
                    ["created"] = "created_at",
                    ["externalid"] = "external_id",
                    ["opens"] = "opens",
                    ["notes"] = "notes",
                    ["code"] = "code",
                },
                new[]
                {
                    new CollectionSqlMapping(
                        "tags",
                        "\"widget_tag\"",
                        "\"widget_tag\".\"widget_id\" = " + parentId,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["label"] = "label",
                            ["score"] = "score",
                        },
                        new[]
                        {
                            new CollectionSqlMapping(
                                "tag_meta",
                                "\"widget_tag_meta\"",
                                "\"widget_tag_meta\".\"tag_id\" = \"widget_tag\".\"id\"",
                                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                {
                                    ["kind"] = "kind",
                                    ["value"] = "value",
                                }),
                        }),
                });
        }

        public static SqlQueryMapping TagsOnly()
        {
            return new SqlQueryMapping(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["label"] = "label",
                    ["score"] = "score",
                });
        }

        public static SqlQueryMapping TagMetaOnly()
        {
            return new SqlQueryMapping(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["kind"] = "kind",
                    ["value"] = "value",
                });
        }
    }
}
