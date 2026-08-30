using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Parsing;

namespace Expresso.Tests.Parsing
{
    public class CollectionQueryParserTests
    {
        private readonly FilterParser _parser = new();
        private readonly SortDirectiveParser _sortParser = new();
        private readonly QueryModel _bookModel = CreateBookModel();

        [Fact]
        public void Parse_AnyAuthorsEqDisplayName_ReturnsAnyFunc()
        {
            var result = _parser.Parse("any(authors, eq(displayname, \"Leo Tolstoy\"))", _bookModel);

            var any = Assert.IsType<AnyFunc>(result.Expression);
            Assert.Equal("authors", any.Collection.Name);
            var eq = Assert.IsType<EqFunc>(any.Predicate);
            var field = Assert.IsType<Field>(eq.Arguments[0]);
            Assert.Equal("displayname", field.Name);
            Assert.Equal("authors", field.Scope);
            var literal = Assert.IsType<Literal>(eq.Arguments[1]);
            Assert.Equal("Leo Tolstoy", literal.Value);
        }

        [Fact]
        public void Parse_EqCountAuthors_ReturnsCollectionCountFunc()
        {
            var result = _parser.Parse("eq(count(authors), 2)", _bookModel);

            var eq = Assert.IsType<EqFunc>(result.Expression);
            var count = Assert.IsType<CollectionCountFunc>(eq.Arguments[0]);
            Assert.Equal("authors", count.Collection.Name);
            Assert.Null(count.Predicate);
            Assert.Equal(2, Assert.IsType<Literal>(eq.Arguments[1]).Value);
        }

        [Fact]
        public void Parse_AnyAuthorsOneArg_HasNoPredicate()
        {
            var result = _parser.Parse("any(authors)", _bookModel);

            var any = Assert.IsType<AnyFunc>(result.Expression);
            Assert.Null(any.Predicate);
        }

        [Fact]
        public void Parse_NestedAnyAuthorsAwards_UsesInnerScope()
        {
            var result = _parser.Parse("any(authors, any(awards, eq(name, \"Nobel Prize\")))", _bookModel);

            var outer = Assert.IsType<AnyFunc>(result.Expression);
            var inner = Assert.IsType<AnyFunc>(outer.Predicate);
            Assert.Equal("awards", inner.Collection.Name);
            Assert.Equal("authors", inner.Collection.Scope);
            var eq = Assert.IsType<EqFunc>(inner.Predicate);
            var field = Assert.IsType<Field>(eq.Arguments[0]);
            Assert.Equal("name", field.Name);
            Assert.Equal("authors.awards", field.Scope);
        }

        [Fact]
        public void Parse_MinAuthorsDateOfBirth_ReturnsCollectionMinFunc()
        {
            var result = _parser.Parse("gt(min(authors, dateofbirth), \"1828-01-01\")", _bookModel);

            var gt = Assert.IsType<GtFunc>(result.Expression);
            var min = Assert.IsType<CollectionMinFunc>(gt.Arguments[0]);
            Assert.Equal("authors", min.Collection.Name);
            var selector = Assert.IsType<Field>(min.Selector);
            Assert.Equal("dateofbirth", selector.Name);
            Assert.Equal(typeof(DateTime), selector.ReturnType);
        }

        [Fact]
        public void Parse_MinPriceRating_ReturnsScalarMinFunc()
        {
            var result = _parser.Parse("eq(min(price, rating), rating)", _bookModel);

            var eq = Assert.IsType<EqFunc>(result.Expression);
            Assert.IsType<MinFunc>(eq.Arguments[0]);
        }

        [Fact]
        public void Parse_DisplayNameAtBookRoot_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => _parser.Parse("eq(displayname, \"Leo Tolstoy\")", _bookModel));
            Assert.Contains("Illegal field name", ex.Message);
        }

        [Fact]
        public void Parse_UnknownCollection_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => _parser.Parse("any(tags)", _bookModel));
            Assert.Contains("Illegal field name", ex.Message);
        }

        [Fact]
        public void Parse_SumOnScalarField_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => _parser.Parse("eq(sum(price, rating), 1)", _bookModel));
            Assert.Contains("must be a collection", ex.Message);
        }

        [Fact]
        public void Parse_AllWithoutPredicate_ReturnsAllFunc()
        {
            var result = _parser.Parse("all(authors)", _bookModel);

            var all = Assert.IsType<AllFunc>(result.Expression);
            Assert.Null(all.Predicate);
        }

        [Fact]
        public void Parse_SortCountAuthors_IsAllowed()
        {
            var result = _sortParser.Parse("count(authors),desc", _bookModel);

            Assert.Single(result.Items);
            Assert.IsType<CollectionCountFunc>(result.Items[0].Expression);
        }

        [Fact]
        public void Parse_SortAnyAuthors_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => _sortParser.Parse("any(authors),asc", _bookModel));
            Assert.Contains("sort keys", ex.Message);
        }

        private static QueryModel CreateBookModel()
        {
            var awards = new QueryModel(new (string, Type)[]
            {
                ("name", typeof(string)),
                ("year", typeof(int)),
            });
            var authors = new QueryModel(
                new (string, Type)[]
                {
                    ("firstname", typeof(string)),
                    ("displayname", typeof(string)),
                    ("dateofbirth", typeof(DateTime)),
                    ("score", typeof(int)),
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
