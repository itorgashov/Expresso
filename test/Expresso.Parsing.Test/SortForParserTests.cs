using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Parsing;

namespace Expresso.Tests.Parsing
{
    public class SortForParserTests
    {
        private readonly SortDirectiveParser _parser = new();
        private readonly FilterParser _filterParser = new();
        private readonly QueryModel _bookModel = CreateBookModel();

        [Fact]
        public void Parse_SortForAuthorsLastname_PutsKeyInNested()
        {
            var result = _parser.Parse("sortfor(authors, lastname),asc", _bookModel);

            Assert.Empty(result.Items);
            Assert.Single(result.Nested);
            Assert.Equal("authors", result.Nested[0].Name);
            var item = Assert.Single(result.Nested[0].Directive.Items);
            var field = Assert.IsType<Field>(item.Expression);
            Assert.Equal("lastname", field.Name);
            Assert.Equal("authors", field.Scope);
            Assert.Equal(SortDirection.Ascending, item.Direction);
        }

        [Fact]
        public void Parse_MixedParentAndSortFor_SplitsItemsAndNested()
        {
            var result = _parser.Parse("year,desc,sortfor(authors, lastname),asc", _bookModel);

            Assert.Single(result.Items);
            Assert.Equal(SortDirection.Descending, result.Items[0].Direction);
            Assert.Single(result.Nested);
            Assert.Single(result.Nested[0].Directive.Items);
        }

        [Fact]
        public void Parse_SortForAuthorsAwardsTitle_NestsAwardsUnderAuthors()
        {
            var result = _parser.Parse("sortfor(authors/awards, title),desc", _bookModel);

            Assert.Empty(result.Items);
            var authors = Assert.Single(result.Nested);
            Assert.Equal("authors", authors.Name);
            Assert.Empty(authors.Directive.Items);
            var awards = Assert.Single(authors.Directive.Nested);
            Assert.Equal("awards", awards.Name);
            var item = Assert.Single(awards.Directive.Items);
            var field = Assert.IsType<Field>(item.Expression);
            Assert.Equal("title", field.Name);
            Assert.Equal("authors.awards", field.Scope);
            Assert.Equal(SortDirection.Descending, item.Direction);
        }

        [Fact]
        public void Parse_SortForWithBooleanExpression_ParsesInItemScope()
        {
            var result = _parser.Parse("sortfor(authors, gt(len(lastname),10)),desc", _bookModel);

            var item = Assert.Single(result.Nested[0].Directive.Items);
            Assert.IsType<GtFunc>(item.Expression);
            Assert.Equal(SortDirection.Descending, item.Direction);
        }

        [Fact]
        public void Parse_SortForLeadingSlash_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => _parser.Parse("sortfor(/authors, lastname),asc", _bookModel));
            Assert.Contains("must not start with '/'", ex.Message);
        }

        [Fact]
        public void Parse_SortForUnknownPath_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => _parser.Parse("sortfor(tags, name),asc", _bookModel));
            Assert.Contains("Illegal field name", ex.Message);
        }

        [Fact]
        public void Parse_CountAuthors_RemainsParentSortKey()
        {
            var result = _parser.Parse("count(authors),desc", _bookModel);

            Assert.Single(result.Items);
            Assert.Empty(result.Nested);
            Assert.IsType<CollectionCountFunc>(result.Items[0].Expression);
        }

        [Fact]
        public void Parse_MultipleSortForSamePath_AppendsToSameNode()
        {
            var result = _parser.Parse(
                "sortfor(authors, lastname),asc,sortfor(authors, firstname),desc",
                _bookModel);

            Assert.Equal(2, result.Nested[0].Directive.Items.Count);
            Assert.Equal("lastname", ((Field)result.Nested[0].Directive.Items[0].Expression).Name);
            Assert.Equal("firstname", ((Field)result.Nested[0].Directive.Items[1].Expression).Name);
        }

        [Fact]
        public void Parse_FilterSortFor_ThrowsDedicatedError()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => _filterParser.Parse("sortfor(authors, lastname)", _bookModel));
            Assert.Equal("'sortfor' is only valid in a sort directive, not in a filter.", ex.Message);
        }

        private static QueryModel CreateBookModel()
        {
            var awards = new QueryModel(new (string, Type)[]
            {
                ("title", typeof(string)),
                ("year", typeof(int)),
            });
            var authors = new QueryModel(
                new (string, Type)[]
                {
                    ("firstname", typeof(string)),
                    ("lastname", typeof(string)),
                    ("displayname", typeof(string)),
                    ("dateofbirth", typeof(DateTime)),
                },
                new[] { new CollectionModel("awards", awards) });

            return new QueryModel(
                new (string, Type)[]
                {
                    ("title", typeof(string)),
                    ("year", typeof(int)),
                    ("price", typeof(double)),
                    ("rating", typeof(double)),
                },
                new[] { new CollectionModel("authors", authors) });
        }
    }
}
