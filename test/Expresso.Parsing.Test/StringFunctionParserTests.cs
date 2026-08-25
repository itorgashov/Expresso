using Expresso.Core.CriteriaExpressions;
using Expresso.Parsing;

namespace Expresso.Tests.Parsing
{
    public class StringFunctionParserTests
    {
        private readonly FilterParser _parser;
        private readonly (string, Type)[] _validFields =
        [
            ("name", typeof(string)),
            ("foo", typeof(string)),
            ("age", typeof(int))
        ];

        public StringFunctionParserTests()
        {
            _parser = new FilterParser();
        }

        [Fact]
        public void Parse_Contains_ReturnsStrContainsFunc()
        {
            var result = _parser.Parse("contains(name,\"war\")", _validFields);

            Assert.IsType<StrContainsFunc>(result.Expression);
            var func = (StrContainsFunc)result.Expression!;
            Assert.IsType<Field>(func.Arguments[0]);
            Assert.IsType<Literal>(func.Arguments[1]);
            Assert.Equal("war", ((Literal)func.Arguments[1]).Value);
        }

        [Fact]
        public void Parse_EndsWith_ReturnsStrEndswithFunc()
        {
            var result = _parser.Parse("endswith(name,\"hn\")", _validFields);
            Assert.IsType<StrEndswithFunc>(result.Expression);
        }

        [Fact]
        public void Parse_SubstrAlias_ReturnsSubStringFunc()
        {
            var result = _parser.Parse("eq(substr(name,1,2),\"Jo\")", _validFields);
            var eq = Assert.IsType<EqFunc>(result.Expression);
            Assert.IsType<SubStringFunc>(eq.Arguments[0]);
        }

        [Fact]
        public void Parse_Left_ReturnsLeftFunc()
        {
            var result = _parser.Parse("eq(left(name,1),\"J\")", _validFields);
            var eq = Assert.IsType<EqFunc>(result.Expression);
            Assert.IsType<LeftFunc>(eq.Arguments[0]);
        }

        [Fact]
        public void Parse_Right_ReturnsRightFunc()
        {
            var result = _parser.Parse("eq(right(name,1),\"n\")", _validFields);
            var eq = Assert.IsType<EqFunc>(result.Expression);
            Assert.IsType<RightFunc>(eq.Arguments[0]);
        }

        [Fact]
        public void Parse_Concat_ReturnsConcatFunc()
        {
            var result = _parser.Parse("eq(concat(name,\" \",foo),\"John Doe\")", _validFields);
            var eq = Assert.IsType<EqFunc>(result.Expression);
            var concat = Assert.IsType<ConcatFunc>(eq.Arguments[0]);
            Assert.Equal(3, concat.Arguments.Count);
        }

        [Fact]
        public void Parse_LowerUpperTrim_ReturnsExpectedTypes()
        {
            Assert.IsType<LowerFunc>(Assert.IsType<EqFunc>(_parser.Parse("eq(lower(name),\"john\")", _validFields).Expression).Arguments[0]);
            Assert.IsType<UpperFunc>(Assert.IsType<EqFunc>(_parser.Parse("eq(upper(name),\"JOHN\")", _validFields).Expression).Arguments[0]);
            Assert.IsType<TrimFunc>(Assert.IsType<EqFunc>(_parser.Parse("eq(trim(name),\"John\")", _validFields).Expression).Arguments[0]);
            Assert.IsType<LTrimFunc>(Assert.IsType<EqFunc>(_parser.Parse("eq(ltrim(name),\"John\")", _validFields).Expression).Arguments[0]);
            Assert.IsType<RTrimFunc>(Assert.IsType<EqFunc>(_parser.Parse("eq(rtrim(name),\"John\")", _validFields).Expression).Arguments[0]);
        }

        [Fact]
        public void Parse_Len_ReturnsLenFuncWithIntComparison()
        {
            var result = _parser.Parse("eq(len(name),4)", _validFields);
            var eq = Assert.IsType<EqFunc>(result.Expression);
            Assert.IsType<LenFunc>(eq.Arguments[0]);
            Assert.Equal(4, ((Literal)eq.Arguments[1]).Value);
        }

        [Fact]
        public void Parse_Replace_ReturnsReplaceFunc()
        {
            var result = _parser.Parse("eq(replace(name,\"J\",\"K\"),\"Kohn\")", _validFields);
            var eq = Assert.IsType<EqFunc>(result.Expression);
            Assert.IsType<ReplaceFunc>(eq.Arguments[0]);
        }

        [Fact]
        public void Parse_IndexOf_ReturnsIndexOfFunc()
        {
            var result = _parser.Parse("gt(indexof(name,\"o\"),0)", _validFields);
            var gt = Assert.IsType<GtFunc>(result.Expression);
            Assert.IsType<IndexOfFunc>(gt.Arguments[0]);
        }

        [Fact]
        public void Parse_Concat_TooFewArguments_ThrowsException()
        {
            Assert.Throws<Exception>(() => _parser.Parse("eq(concat(name),\"x\")", _validFields));
        }

        [Fact]
        public void Parse_Contains_WrongArity_ThrowsException()
        {
            Assert.Throws<Exception>(() => _parser.Parse("contains(name)", _validFields));
        }
    }
}
