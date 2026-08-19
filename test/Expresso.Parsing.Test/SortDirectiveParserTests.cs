using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Sorting;
using Expresso.Parsing;

namespace Expresso.Tests.Parsing
{

    public class SortDirectiveParserTests
    {
        private readonly SortDirectiveParser _parser;
        private readonly (string, Type)[] _validFields =
            {
                ("name", typeof(string)),
                ("age", typeof(int)),
                ("salary", typeof(double))
            };

        public SortDirectiveParserTests()
        {
            _parser = new SortDirectiveParser();
        }

        [Fact]
        public void Parse_ValidSingleSortDirective_ReturnsCorrectSortDirective()
        {
            var query = "name,asc";

            var result = _parser.Parse(query, _validFields);

            Assert.Single(result.Items);
            Assert.IsType<Field>(result.Items[0].Expression);
            Assert.Equal("name", ((Field)result.Items[0].Expression).Name);
            Assert.Equal(SortDirection.Ascending, result.Items[0].Direction);
        }

        [Fact]
        public void Parse_ValidMultipleSortDirectives_ReturnsCorrectSortDirective()
        {
            var query = "name,asc,age,desc";

            var result = _parser.Parse(query, _validFields);

            Assert.Equal(2, result.Items.Count);

            Assert.IsType<Field>(result.Items[0].Expression);
            Assert.Equal("name", ((Field)result.Items[0].Expression).Name);
            Assert.Equal(SortDirection.Ascending, result.Items[0].Direction);

            Assert.IsType<Field>(result.Items[1].Expression);
            Assert.Equal("age", ((Field)result.Items[1].Expression).Name);
            Assert.Equal(SortDirection.Descending, result.Items[1].Direction);
        }

        [Fact]
        public void Parse_ValidFunctionExpression_ReturnsCorrectSortDirective()
        {
            var query = "substring(name, 1, 2),asc";

            var result = _parser.Parse(query, _validFields);

            Assert.Single(result.Items);
            Assert.IsType<SubStringFunc>(result.Items[0].Expression);
            Assert.Equal(SortDirection.Ascending, result.Items[0].Direction);
        }

        [Fact]
        public void Parse_InvalidSortDirection_ThrowsNotSupportedException()
        {
            var query = "name,invalid";

            var ex = Assert.Throws<NotSupportedException>(() => _parser.Parse(query, _validFields));
            Assert.Contains("Unrecodnized sorting direction marker", ex.Message);
        }

        [Fact]
        public void Parse_MissingSortDirection_ThrowsArgumentException()
        {
            var query = "name";

            var ex = Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));
            Assert.Contains("Unexpected end of sort directive", ex.Message);
        }

        [Fact]
        public void Parse_OddNumberOfTokens_ThrowsArgumentException()
        {
            var query = "name,asc,age";

            var ex = Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));
            Assert.Contains("Unexpected end of sort directive", ex.Message);
        }

        [Fact]
        public void Parse_EmptyInput_ThrowsArgumentException()
        {
            var query = "";

            var ex = Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));

            Assert.Contains("Unexpected end of sort directive", ex.Message);
        }

        [Fact]
        public void Parse_InvalidFieldName_ThrowsArgumentException()
        {
            var query = "invalidField,asc";

            var ex = Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));

            Assert.Contains("Illegal field name", ex.Message);
        }

        [Fact]
        public void Parse_ComplexExpressionWithCommas_ReturnsCorrectSortDirective()
        {
            var query = "substring(name, 1, 2),asc,age,desc";

            var result = _parser.Parse(query, _validFields);

            Assert.Equal(2, result.Items.Count);

            Assert.IsType<SubStringFunc>(result.Items[0].Expression);
            Assert.Equal(SortDirection.Ascending, result.Items[0].Direction);

            Assert.IsType<Field>(result.Items[1].Expression);
            Assert.Equal("age", ((Field)result.Items[1].Expression).Name);
            Assert.Equal(SortDirection.Descending, result.Items[1].Direction);
        }
    }
}
