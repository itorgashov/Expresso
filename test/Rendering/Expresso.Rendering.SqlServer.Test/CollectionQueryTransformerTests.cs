using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.SqlServer;

namespace Expresso.Tests.SqlServer
{
    public class CollectionQueryTransformerTests
    {
        private readonly ExpressionToSqlServerQueryClauseTransformer _transformer = new();
        private readonly SqlQueryMapping _mapping = CreateMapping();
        private const string ParamPrefix = "param";

        [Fact]
        public void RenderWhereClause_AnyAuthors_RendersExists()
        {
            var filter = Filter(new AnyFunc(
                Authors(),
                new EqFunc(new Field("displayname", typeof(string), "authors"), new Literal("Leo Tolstoy"))));

            var result = _transformer.RenderWhereClause(filter, _mapping, ParamPrefix);

            Assert.Equal(
                "EXISTS (SELECT 1 FROM dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id WHERE ba.book_id = b.id AND ([a].[display_name] = @param_0))",
                result.whereClause);
            Assert.Equal("Leo Tolstoy", result.parameters["@param_0"]);
        }

        [Fact]
        public void RenderWhereClause_NoneAuthors_RendersNotExists()
        {
            var result = _transformer.RenderWhereClause(Filter(new NoneFunc(Authors())), _mapping, ParamPrefix);

            Assert.Equal(
                "NOT EXISTS (SELECT 1 FROM dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id WHERE ba.book_id = b.id)",
                result.whereClause);
            Assert.Empty(result.parameters);
        }

        [Fact]
        public void RenderWhereClause_AllAuthorsPredicate_RendersNotExistsNotPredicate()
        {
            var filter = Filter(new AllFunc(
                Authors(),
                new EqFunc(new Field("displayname", typeof(string), "authors"), new Literal("Leo Tolstoy"))));

            var result = _transformer.RenderWhereClause(filter, _mapping, ParamPrefix);

            Assert.Equal(
                "NOT EXISTS (SELECT 1 FROM dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id WHERE ba.book_id = b.id AND NOT (([a].[display_name] = @param_0)))",
                result.whereClause);
        }

        [Fact]
        public void RenderWhereClause_AllAuthorsNoPredicate_RendersTautology()
        {
            var result = _transformer.RenderWhereClause(Filter(new AllFunc(Authors())), _mapping, ParamPrefix);

            Assert.Equal("(1 = 1)", result.whereClause);
        }

        [Fact]
        public void RenderWhereClause_CountAuthors_RendersScalarSubquery()
        {
            var filter = Filter(new EqFunc(new CollectionCountFunc(Authors()), new Literal(2)));

            var result = _transformer.RenderWhereClause(filter, _mapping, ParamPrefix);

            Assert.Equal(
                "((SELECT COUNT(*) FROM dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id WHERE ba.book_id = b.id) = @param_0)",
                result.whereClause);
            Assert.Equal(2, result.parameters["@param_0"]);
        }

        [Fact]
        public void RenderWhereClause_MinAuthors_RendersMinSubquery()
        {
            var birth = new DateTime(1828, 1, 1);
            var filter = Filter(new GtFunc(
                new CollectionMinFunc(Authors(), new Field("dateofbirth", typeof(DateTime), "authors")),
                new Literal(birth)));

            var result = _transformer.RenderWhereClause(filter, _mapping, ParamPrefix);

            Assert.Equal(
                "((SELECT MIN([a].[date_of_birth]) FROM dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id WHERE ba.book_id = b.id) > @param_0)",
                result.whereClause);
            Assert.Equal(birth, result.parameters["@param_0"]);
        }

        [Fact]
        public void RenderWhereClause_OuterYearAndAny_KeepsYearOutsideSubquery()
        {
            var filter = Filter(new AndFunc(new List<AbstractExpression>
            {
                new GtFunc(new Field("year", typeof(int)), new Literal(2020)),
                new AnyFunc(
                    Authors(),
                    new EqFunc(new Field("displayname", typeof(string), "authors"), new Literal("Leo Tolstoy"))),
            }));

            var result = _transformer.RenderWhereClause(filter, _mapping, ParamPrefix);

            Assert.Equal(
                "(([b].[year] > @param_0) AND EXISTS (SELECT 1 FROM dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id WHERE ba.book_id = b.id AND ([a].[display_name] = @param_1)))",
                result.whereClause);
            Assert.Equal(2020, result.parameters["@param_0"]);
            Assert.Equal("Leo Tolstoy", result.parameters["@param_1"]);
            var existsSql = result.whereClause.Substring(result.whereClause.IndexOf("EXISTS", StringComparison.Ordinal));
            Assert.DoesNotContain("[b].[year]", existsSql);
        }

        [Fact]
        public void RenderWhereClause_NestedAny_RendersNestedExists()
        {
            var filter = Filter(new AnyFunc(
                Authors(),
                new AnyFunc(
                    new CollectionRef("awards", "authors"),
                    new EqFunc(new Field("name", typeof(string), "authors.awards"), new Literal("Nobel Prize")))));

            var result = _transformer.RenderWhereClause(filter, _mapping, ParamPrefix);

            Assert.Equal(
                "EXISTS (SELECT 1 FROM dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id WHERE ba.book_id = b.id AND EXISTS (SELECT 1 FROM dbo.author_award AS aw WHERE aw.author_id = a.id AND ([aw].[name] = @param_0)))",
                result.whereClause);
            Assert.Equal("Nobel Prize", result.parameters["@param_0"]);
        }

        [Fact]
        public void RenderWhereClause_MissingCollectionMapping_ThrowsArgumentException()
        {
            var filter = Filter(new AnyFunc(Authors()));
            var scalarOnly = new SqlQueryMapping(new Dictionary<string, string> { ["year"] = "b.year" });

            var ex = Assert.Throws<ArgumentException>(
                () => _transformer.RenderWhereClause(filter, scalarOnly, ParamPrefix));
            Assert.Contains("No mapping for the authors collection", ex.Message);
        }

        [Fact]
        public void RenderOrderByClause_CountAuthors_RendersScalarSubquery()
        {
            var sort = new SortDirective(new List<SortDirectiveItem>
            {
                new() { Expression = new CollectionCountFunc(Authors()), Direction = SortDirection.Descending },
            });

            var result = _transformer.RenderOrderByClause(sort, _mapping, ParamPrefix);

            Assert.Equal(
                "(SELECT COUNT(*) FROM dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id WHERE ba.book_id = b.id) DESC",
                result.orderByClause);
        }

        [Fact]
        public void RenderOrderByClause_AnyAuthors_ThrowsArgumentException()
        {
            var sort = new SortDirective(new List<SortDirectiveItem>
            {
                new() { Expression = new AnyFunc(Authors()), Direction = SortDirection.Ascending },
            });

            var ex = Assert.Throws<ArgumentException>(
                () => _transformer.RenderOrderByClause(sort, _mapping, ParamPrefix));
            Assert.Contains("sort keys", ex.Message);
        }

        [Fact]
        public void RenderOrderByClause_AuthorLastname_RendersColumn()
        {
            var authorMapping = new SqlQueryMapping(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["lastname"] = "a.last_name",
            });
            var sort = new SortDirective(new List<SortDirectiveItem>
            {
                new()
                {
                    Expression = new Field("lastname", typeof(string), "authors"),
                    Direction = SortDirection.Ascending,
                },
            });

            var result = _transformer.RenderOrderByClause(sort, authorMapping, ParamPrefix);

            Assert.Equal("[a].[last_name] ASC", result.orderByClause);
        }

        [Fact]
        public void RenderOrderByClause_AwardTitle_RendersColumn()
        {
            var awardMapping = new SqlQueryMapping(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["title"] = "aw.title",
            });
            var sort = new SortDirective(new List<SortDirectiveItem>
            {
                new()
                {
                    Expression = new Field("title", typeof(string), "authors.awards"),
                    Direction = SortDirection.Descending,
                },
            });

            var result = _transformer.RenderOrderByClause(sort, awardMapping, ParamPrefix);

            Assert.Equal("[aw].[title] DESC", result.orderByClause);
        }

        [Fact]
        public void RenderOrderByClause_ParentIgnoresNested()
        {
            var sort = new SortDirective(
                new List<SortDirectiveItem>
                {
                    new() { Expression = new Field("year", typeof(int)), Direction = SortDirection.Descending },
                },
                new[]
                {
                    new CollectionSort(
                        "authors",
                        new SortDirective(new List<SortDirectiveItem>
                        {
                            new()
                            {
                                Expression = new Field("lastname", typeof(string), "authors"),
                                Direction = SortDirection.Ascending,
                            },
                        })),
                });

            var result = _transformer.RenderOrderByClause(sort, _mapping, ParamPrefix);

            Assert.Equal("[b].[year] DESC", result.orderByClause);
        }

        private static CollectionRef Authors() => new("authors");

        private static FilterCriteria Filter(BooleanFunction expression) =>
            new() { Expression = expression };

        private static SqlQueryMapping CreateMapping()
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
                                }),
                        }),
                });
        }
    }
}
