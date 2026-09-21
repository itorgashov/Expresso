using Expresso.Rendering;

namespace Expresso.Rendering.TestCases
{
    public static class RendererMaps
    {
        public const string ParamPrefix = "param";

        public static Dictionary<string, string> Standard { get; } = new()
        {
            { "name", "name_col" },
            { "age", "age_col" },
            { "salary", "salary_col" },
            { "foo", "foo_col" },
        };

        public static Dictionary<string, string> Numeric { get; } = new()
        {
            { "age", "p.age" },
            { "salary", "p.salary" },
        };

        public static Dictionary<string, string> DateTime { get; } = new()
        {
            { "createdat", "b.created_at" },
            { "id", "b.id" },
        };

        public static SqlQueryMapping BookAuthors()
        {
            return new SqlQueryMapping(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["year"] = "b.year",
                    ["title"] = "b.title",
                },
                new[]
                {
                    new CollectionSqlMapping(
                        "authors",
                        "dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id",
                        "ba.book_id = b.id",
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["displayname"] = "a.display_name",
                            ["dateofbirth"] = "a.date_of_birth",
                            ["lastname"] = "a.last_name",
                        },
                        new[]
                        {
                            new CollectionSqlMapping(
                                "awards",
                                "dbo.author_award AS aw",
                                "aw.author_id = a.id",
                                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                {
                                    ["name"] = "aw.name",
                                    ["year"] = "aw.year",
                                    ["title"] = "aw.title",
                                }),
                        }),
                });
        }
    }
}
