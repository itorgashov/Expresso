using Expresso.Core.CriteriaExpressions;
using Expresso.Parsing;

namespace Expresso.Tests.Parsing
{
    public class LiteralParseOptionsTests
    {
        private readonly (string, Type)[] _dateTimeFields =
        [
            ("dateFrom", typeof(DateTime)),
        ];

        private readonly (string, Type)[] _timeSpanFields =
        [
            ("opens", typeof(TimeSpan)),
        ];

        [Fact]
        public void Parse_DefaultOptions_IsoDateTimeStillWorks()
        {
            var parser = new FilterParser();

            var result = parser.Parse("eq(dateFrom, \"1899-12-31\")", _dateTimeFields);

            var eqFunc = Assert.IsType<EqFunc>(result.Expression);
            var literal = Assert.IsType<Literal>(eqFunc.Arguments[1]);
            Assert.Equal(new DateTime(1899, 12, 31), literal.Value);
        }

        [Fact]
        public void Parse_CustomDateTimeFormats_DutchCulture_ParsesDayMonthYear()
        {
            var options = new LiteralParseOptions
            {
                CultureName = "nl-NL",
                DateTimeFormats = new[] { "dd-MM-yyyy", "yyyy-MM-dd" },
            };
            var parser = new FilterParser(options);

            var result = parser.Parse("eq(dateFrom, \"31-12-1899\")", _dateTimeFields);

            var eqFunc = Assert.IsType<EqFunc>(result.Expression);
            var literal = Assert.IsType<Literal>(eqFunc.Arguments[1]);
            Assert.Equal(new DateTime(1899, 12, 31), literal.Value);
        }

        [Fact]
        public void Parse_FallbackDisabled_NonIsoDateTime_Throws()
        {
            var options = new LiteralParseOptions
            {
                AllowCultureFallback = false,
                DateTimeFormats = new[] { "yyyy-MM-dd" },
            };
            var parser = new FilterParser(options);

            Assert.Throws<ArgumentException>(() => parser.Parse("eq(dateFrom, \"31-12-1899\")", _dateTimeFields));
        }

        [Fact]
        public void Constructor_InvalidCultureName_Throws()
        {
            var options = new LiteralParseOptions { CultureName = "not-a-real-culture-xyz" };

            Assert.Throws<ArgumentException>(() => new FilterParser(options));
        }

        [Fact]
        public void Parse_CustomTimeSpanFormats_AllowsSingleDigitHour()
        {
            var options = new LiteralParseOptions
            {
                TimeSpanFormats = new[] { @"h\:mm", @"hh\:mm" },
            };
            var parser = new FilterParser(options);

            var result = parser.Parse("eq(opens, \"9:00\")", _timeSpanFields);

            var eqFunc = Assert.IsType<EqFunc>(result.Expression);
            var literal = Assert.IsType<Literal>(eqFunc.Arguments[1]);
            Assert.Equal(new TimeSpan(9, 0, 0), literal.Value);
        }

#if NET6_0_OR_GREATER
        private readonly (string, Type)[] _timeOnlyFields =
        [
            ("starts", typeof(TimeOnly)),
        ];

        [Fact]
        public void Parse_CustomTimeOnlyFormats_AllowsSingleDigitHour()
        {
            var options = new LiteralParseOptions
            {
                TimeFormats = new[] { "H:mm", "HH:mm" },
            };
            var parser = new FilterParser(options);

            var result = parser.Parse("eq(starts, \"9:00\")", _timeOnlyFields);

            var eqFunc = Assert.IsType<EqFunc>(result.Expression);
            var literal = Assert.IsType<Literal>(eqFunc.Arguments[1]);
            Assert.Equal(new TimeOnly(9, 0), literal.Value);
        }
#endif
    }
}
