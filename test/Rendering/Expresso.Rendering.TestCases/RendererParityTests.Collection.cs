using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.Rendering.TestCases
{
    public abstract partial class RendererParityTests
    {
        [Fact]
        public void RenderWhereClause_AnyAuthors_RendersExists()
        {
            var filter = new FilterCriteria
            {
                Expression = new AnyFunc(
                    new CollectionRef("authors"),
                    new EqFunc(new Field("displayname", typeof(string), "authors"), new Literal("Leo Tolstoy")))
            };
            var result = T.RenderWhereClause(filter, RendererMaps.BookAuthors(), D.Prefix);
            Assert.Equal(
                $"EXISTS (SELECT 1 FROM dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id WHERE ba.book_id = b.id AND ({D.Q("a.display_name")} = {D.P(0)}))",
                result.whereClause);
        }

        [Fact]
        public void RenderWhereClause_AllNoneCountMin_RenderPortableSubqueries()
        {
            var mapping = RendererMaps.BookAuthors();
            Assert.Equal(
                "NOT EXISTS (SELECT 1 FROM dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id WHERE ba.book_id = b.id)",
                T.RenderWhereClause(new() { Expression = new NoneFunc(new CollectionRef("authors")) }, mapping, D.Prefix).whereClause);
            Assert.Equal("(1 = 1)",
                T.RenderWhereClause(new() { Expression = new AllFunc(new CollectionRef("authors")) }, mapping, D.Prefix).whereClause);

            var count = T.RenderWhereClause(
                new() { Expression = new EqFunc(new CollectionCountFunc(new CollectionRef("authors")), new Literal(2)) },
                mapping,
                D.Prefix);
            Assert.Equal(
                $"((SELECT COUNT(*) FROM dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id WHERE ba.book_id = b.id) = {D.P(0)})",
                count.whereClause);

            var nested = T.RenderWhereClause(
                new()
                {
                    Expression = new AnyFunc(
                        new CollectionRef("authors"),
                        new AnyFunc(
                            new CollectionRef("awards", "authors"),
                            new EqFunc(new Field("name", typeof(string), "authors.awards"), new Literal("Nobel Prize"))))
                },
                mapping,
                D.Prefix);
            Assert.Contains("EXISTS (SELECT 1 FROM dbo.author_award AS aw WHERE aw.author_id = a.id", nested.whereClause);
        }

        [Fact]
        public void RenderOrderByClause_CountAuthors_RendersScalarSubquery()
        {
            var sort = new SortDirective(new List<SortDirectiveItem>
            {
                new() { Expression = new CollectionCountFunc(new CollectionRef("authors")), Direction = SortDirection.Descending }
            });
            Assert.Equal(
                "(SELECT COUNT(*) FROM dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id WHERE ba.book_id = b.id) DESC",
                T.RenderOrderByClause(sort, RendererMaps.BookAuthors(), D.Prefix).orderByClause);
        }

        [Fact]
        public void RenderOrderByClause_AnyAuthors_ThrowsArgumentException()
        {
            var sort = new SortDirective(new List<SortDirectiveItem>
            {
                new() { Expression = new AnyFunc(new CollectionRef("authors")), Direction = SortDirection.Ascending }
            });
            var ex = Assert.Throws<ArgumentException>(() => T.RenderOrderByClause(sort, RendererMaps.BookAuthors(), D.Prefix));
            Assert.Contains("sort keys", ex.Message);
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
                            new() { Expression = new Field("lastname", typeof(string), "authors"), Direction = SortDirection.Ascending },
                        })),
                });
            Assert.Equal($"{D.Q("b.year")} DESC", T.RenderOrderByClause(sort, RendererMaps.BookAuthors(), D.Prefix).orderByClause);
        }
    }
}
