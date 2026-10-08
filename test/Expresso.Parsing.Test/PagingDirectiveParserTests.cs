using Expresso.Parsing;
using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Tests.Parsing
{
    public class PagingDirectiveParserTests
    {
        private readonly PagingDirectiveParser _parser = new PagingDirectiveParser();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Parse_Whitespace_IsMissing(string? value)
        {
            var paging = _parser.Parse(value, value, value, value);
            Assert.True(paging.IsEmpty);
            Assert.Null(paging.Page);
            Assert.Null(paging.PageSize);
            Assert.Null(paging.Skip);
            Assert.Null(paging.Take);
        }

        [Fact]
        public void Parse_ValidValues_BuildsDirective()
        {
            var paging = _parser.Parse("2", "10", null, null);
            Assert.Equal(2, paging.Page);
            Assert.Equal(10, paging.PageSize);
            Assert.Equal(10, paging.Offset);
            Assert.Equal(10, paging.Limit);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("1.5")]
        [InlineData("-1")]
        [InlineData("+1")]
        [InlineData("1 ")]
        [InlineData("2147483648")]
        public void Parse_RejectsNonIntegers(string value)
        {
            var error = Assert.Throws<ArgumentException>(() => _parser.Parse(value, "10", null, null));
            Assert.Equal("page", error.ParamName);
            Assert.Contains(value, error.Message);
        }

        [Fact]
        public void Parse_RejectsOutOfRange()
        {
            var error = Assert.Throws<ArgumentOutOfRangeException>(() => _parser.Parse("0", "10", null, null));
            Assert.Equal("page", error.ParamName);
        }

        [Fact]
        public void Registration_ResolvesPagingParser()
        {
            var services = new ServiceCollection();
            services.AddRequestParametersParsers();
            var parser = services.BuildServiceProvider().GetRequiredService<IPagingDirectiveParser>();
            Assert.IsType<PagingDirectiveParser>(parser);
        }
    }
}
