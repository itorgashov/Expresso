using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Parsing;

namespace Expresso.Tests.Parsing
{
    public class FilterParserTests
    {
        private readonly FilterParser _parser;
        private readonly (string, Type)[] _validFields;

        public FilterParserTests()
        {
            _parser = new();
            _validFields = [
                ( "name", typeof(string) ),
                ( "age", typeof(int) ),
                ( "status", typeof(byte) ),
                ( "dateFrom", typeof(DateTime) ),
                ( "dateTo", typeof(DateTime) ),
                ( "foo", typeof(string) ),
                ( "doubleField", typeof(double) ),
                ( "file_name", typeof(string) ),
                ( "id", typeof(Guid) )
            ];
        }

        [Fact]
        public void Parse_ValidAndFunction_ReturnsAndFunc()
        {
            var query = "and(eq(name, \"John\"), gt(age, 25))";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<AndFunc>(result.Expression);
            var andFunc = (AndFunc)result.Expression;
            Assert.Equal(2, andFunc.Arguments.Count);
            Assert.IsType<EqFunc>(andFunc.Arguments[0]);
            Assert.IsType<GtFunc>(andFunc.Arguments[1]);
        }

        [Fact]
        public void Parse_ValidOrFunction_ReturnsOrFunc()
        {
            var query = "or(eq(name, \"John\"), gt(age, 25))";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<OrFunc>(result.Expression);
            var orFunc = (OrFunc)result.Expression;
            Assert.Equal(2, orFunc.Arguments.Count);
            Assert.IsType<EqFunc>(orFunc.Arguments[0]);
            Assert.IsType<GtFunc>(orFunc.Arguments[1]);
        }

        [Fact]
        public void Parse_ValidNotFunction_ReturnsNotFunc()
        {
            var query = "not(eq(name, \"John\"))";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<NotFunc>(result.Expression);
            var notFunc = (NotFunc)result.Expression;
            Assert.Single(notFunc.Arguments);
            Assert.IsType<EqFunc>(notFunc.Arguments[0]);
        }

        [Fact]
        public void Parse_ValidEqFunction_ReturnsEqFunc()
        {
            var query = "eq(name, \"John\")";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.Equal(2, eqFunc.Arguments.Count);
            Assert.IsType<Field>(eqFunc.Arguments[0]);
            Assert.IsType<Literal>(eqFunc.Arguments[1]);
        }

        [Fact]
        public void Parse_ValidNeqFunction_ReturnsNeqFunc()
        {
            var query = "neq(name, \"John\")";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<NeqFunc>(result.Expression);
            var neqFunc = (NeqFunc)result.Expression;
            Assert.Equal(2, neqFunc.Arguments.Count);
            Assert.IsType<Field>(neqFunc.Arguments[0]);
            Assert.IsType<Literal>(neqFunc.Arguments[1]);
        }

        [Fact]
        public void Parse_ValidGtFunction_ReturnsGtFunc()
        {
            var query = "gt(age, 25)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<GtFunc>(result.Expression);
            var gtFunc = (GtFunc)result.Expression;
            Assert.Equal(2, gtFunc.Arguments.Count);
            Assert.IsType<Field>(gtFunc.Arguments[0]);
            Assert.IsType<Literal>(gtFunc.Arguments[1]);
        }

        [Fact]
        public void Parse_ValidGteFunction_ReturnsGteFunc()
        {
            var query = "gte(age, 25)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<GteFunc>(result.Expression);
            var gteFunc = (GteFunc)result.Expression;
            Assert.Equal(2, gteFunc.Arguments.Count);
            Assert.IsType<Field>(gteFunc.Arguments[0]);
            Assert.IsType<Literal>(gteFunc.Arguments[1]);
        }

        [Fact]
        public void Parse_ValidLtFunction_ReturnsLtFunc()
        {
            var query = "lt(age, 30)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<LtFunc>(result.Expression);
            var ltFunc = (LtFunc)result.Expression;
            Assert.Equal(2, ltFunc.Arguments.Count);
            Assert.IsType<Field>(ltFunc.Arguments[0]);
            Assert.IsType<Literal>(ltFunc.Arguments[1]);
        }

        [Fact]
        public void Parse_ValidLteFunction_ReturnsLteFunc()
        {
            var query = "lte(age, 30)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<LteFunc>(result.Expression);
            var lteFunc = (LteFunc)result.Expression;
            Assert.Equal(2, lteFunc.Arguments.Count);
            Assert.IsType<Field>(lteFunc.Arguments[0]);
            Assert.IsType<Literal>(lteFunc.Arguments[1]);
        }

        [Fact]
        public void Parse_ValidInFunction_ReturnsInFunc()
        {
            var query = "in(name, \"John\", \"Jane\")";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<InFunc>(result.Expression);
            var inFunc = (InFunc)result.Expression;
            Assert.Equal(3, inFunc.Arguments.Count);
            Assert.IsType<Field>(inFunc.Arguments[0]);
            Assert.IsType<Literal>(inFunc.Arguments[1]);
            Assert.IsType<Literal>(inFunc.Arguments[2]);
        }

        [Fact]
        public void Parse_ValidStartsWithFunction_ReturnsStrStartswithFunc()
        {
            var query = "startswith(name, \"Jo\")";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<StrStartswithFunc>(result.Expression);
            var startsWithFunc = (StrStartswithFunc)result.Expression;
            Assert.Equal(2, startsWithFunc.Arguments.Count);
            Assert.IsType<Field>(startsWithFunc.Arguments[0]);
            Assert.IsType<Literal>(startsWithFunc.Arguments[1]);
        }

        [Fact]
        public void Parse_ValidAbsFunction_ReturnsAbsFunc()
        {
            var query = "eq(abs(age),1)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.Equal(2, eqFunc.Arguments.Count);
            Assert.IsType<AbsFunc>(eqFunc.Arguments[0]);
            var absFunc = (AbsFunc)eqFunc.Arguments[0];
            Assert.IsType<Field>(absFunc.Arguments[0]);
        }

        [Fact]
        public void Parse_ValidAddFunction_ReturnsAddFunc()
        {
            var query = "eq(add(age,1),1)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.Equal(2, eqFunc.Arguments.Count);
            Assert.IsType<AddFunc>(eqFunc.Arguments[0]);
            var addFunc = (AddFunc)eqFunc.Arguments[0];
            Assert.IsType<Field>(addFunc.Arguments[0]);
            Assert.IsType<Literal>(addFunc.Arguments[1]);
        }

        [Fact]
        public void Parse_ValidSubFunction_ReturnsSubFunc()
        {
            var query = "eq(sub(age,1),1)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.Equal(2, eqFunc.Arguments.Count);
            Assert.IsType<SubFunc>(eqFunc.Arguments[0]);
            var subFunc = (SubFunc)eqFunc.Arguments[0];
            Assert.IsType<Field>(subFunc.Arguments[0]);
            Assert.IsType<Literal>(subFunc.Arguments[1]);
        }

        [Fact]
        public void Parse_ValidMultFunction_ReturnsMultFunc()
        {
            var query = "eq(mult(age,1),1)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.Equal(2, eqFunc.Arguments.Count);
            Assert.IsType<MultFunc>(eqFunc.Arguments[0]);
            var multFunc = (MultFunc)eqFunc.Arguments[0];
            Assert.IsType<Field>(multFunc.Arguments[0]);
            Assert.IsType<Literal>(multFunc.Arguments[1]);
        }

        [Fact]
        public void Parse_ValidDivFunction_ReturnsDivFunc()
        {
            var query = "eq(div(age,1),1)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.Equal(2, eqFunc.Arguments.Count);
            Assert.IsType<DivFunc>(eqFunc.Arguments[0]);
            var divFunc = (DivFunc)eqFunc.Arguments[0];
            Assert.IsType<Field>(divFunc.Arguments[0]);
            Assert.IsType<Literal>(divFunc.Arguments[1]);
        }

        [Fact]
        public void Parse_ComplexQuery_ReturnsCorrectExpressionTree()
        {
            var query = "and(or(startswith(name,\"Jo\"),in(substring(name,1,3),\"Mary\",\"Susan\")),gt(age,25),lt(dateFrom,dateTo),not(eq(\"Some Foo\", foo)))";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<AndFunc>(result.Expression);
            var andFunc = (AndFunc)result.Expression;
            Assert.Equal(4, andFunc.Arguments.Count);

            // Check the first argument: or(startswith(name,"Jo"),in(substring(name,1,3),"Mary","Susan"))
            Assert.IsType<OrFunc>(andFunc.Arguments[0]);
            var orFunc = (OrFunc)andFunc.Arguments[0];
            Assert.Equal(2, orFunc.Arguments.Count);

            // Check the second argument: gt(age,25)
            Assert.IsType<GtFunc>(andFunc.Arguments[1]);

            // Check the third argument: lt(dateFrom,dateTo)
            Assert.IsType<LtFunc>(andFunc.Arguments[2]);

            // Check the fourth argument: not(eq("Some Foo", foo))
            Assert.IsType<NotFunc>(andFunc.Arguments[3]);
        }

        [Fact]
        public void Parse_InvalidToken_ThrowsArgumentException()
        {
            var query = "and(.eq(name, \"John\"), gt(age, 25))";

            var exception = Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));
            Assert.Contains("Invalid token", exception.Message);
        }

        [Fact]
        public void Parse_InvalidFunctionName_ThrowsArgumentException()
        {
            var query = "invalid(name, \"John\")";

            var exception = Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));
            Assert.Contains("Unknown function", exception.Message);
        }

        [Fact]
        public void Parse_InvalidFieldName_ThrowsArgumentException()
        {
            var query = "eq(invalidField, \"John\")";

            var exception = Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));
        }

        [Fact]
        public void Parse_InvalidLiteralType_ThrowsArgumentException()
        {
            var query = "eq(name, 123)"; // "name" is a string field, but 123 is an integer

            var exception = Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));
        }

        [Fact]
        public void Parse_EmptyQuery_ReturnsNull()
        {
            var query = "";

            Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));
        }

        [Fact]
        public void Parse_NullQuery_ThrowsArgumentNullException()
        {
            string query = null;

            Assert.Throws<ArgumentNullException>(() => _parser.Parse(query, _validFields));
        }

        [Fact]
        public void Parse_NullValidFields_ThrowsArgumentNullException()
        {
            var query = "eq(name, \"John\")";

            Assert.Throws<ArgumentNullException>(() => _parser.Parse(query, null));
        }

        [Fact]
        public void Parse_DoubleField_ReturnsCorrectExpression()
        {
            var query = "gt(doubleField, 3.14)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<GtFunc>(result.Expression);
            var gtFunc = (GtFunc)result.Expression;
            Assert.Equal(2, gtFunc.Arguments.Count);
            Assert.IsType<Field>(gtFunc.Arguments[0]);
            Assert.IsType<Literal>(gtFunc.Arguments[1]);
            Assert.Equal(3.14, ((Literal)gtFunc.Arguments[1]).Value);
        }

        [Fact]
        public void Parse_CaseInsensitiveFunctionName_ReturnsCorrectExpression()
        {
            var query = "AND(eq(name, \"John\"), gt(age, 25))";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<AndFunc>(result.Expression);
            var andFunc = (AndFunc)result.Expression;
            Assert.Equal(2, andFunc.Arguments.Count);
            Assert.IsType<EqFunc>(andFunc.Arguments[0]);
            Assert.IsType<GtFunc>(andFunc.Arguments[1]);
        }

        [Fact]
        public void Parse_CaseInsensitiveFieldName_ReturnsCorrectExpression()
        {

            var query = "eq(NAME, \"John\")";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.Equal(2, eqFunc.Arguments.Count);
            Assert.IsType<Field>(eqFunc.Arguments[0]);
            Assert.Equal("name", ((Field)eqFunc.Arguments[0]).Name);
        }

        [Fact]
        public void Parse_FieldNameWithUnderscore_ReturnsCorrectExpression()
        {
            var query = "eq(file_name, \"do.exe\")";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.Equal(2, eqFunc.Arguments.Count);
            Assert.IsType<Field>(eqFunc.Arguments[0]);
            Assert.Equal("file_name", ((Field)eqFunc.Arguments[0]).Name);
        }

        [Fact]
        public void Parse_InvalidAndFunction_ThrowsException()
        {
            var query = "and()";

            var exception = Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));
            //Assert.Contains("And() function should at least 2 arguments", exception.Message);
        }

        [Fact]
        public void Parse_InvalidSubstringFunction_MissingArgs_ThrowsException()
        {
            var query = "eq(substring(name, 1), \"J\")";

            var exception = Assert.Throws<Exception>(() => _parser.Parse(query, _validFields));
            //Assert.Contains("Substring() function should have 3 arguments", exception.Message);
        }

        [Fact]
        public void Parse_InvalidSubstringFunction_ExtraArgs_ThrowsException()
        {
            var query = "eq(substring(name, 1, 1, 1), \"J\")";

            var exception = Assert.Throws<Exception>(() => _parser.Parse(query, _validFields));
            //Assert.Contains("Substring() function should have 3 arguments", exception.Message);
        }

        [Fact]
        public void Parse_InvalidParenthesis_ThrowsException()
        {
            var query = "eq((name, \"John\")";

            var exception = Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));
            //Assert.Contains("Unexpected token", exception.Message);
        }

        [Fact]
        public void Parse_InvalidComma_ThrowsException()
        {
            var query = "and(eq(name, \"John\"),)";

            var exception = Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));
            //Assert.Contains("Unexpected token", exception.Message);
        }

        [Fact]
        public void Parse_InvalidClosingParenthesis_ThrowsException()
        {
            var query = "eq(name, \"John\"))";

            var exception = Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));
            //Assert.Contains("Unexpected token", exception.Message);
        }

        [Fact]
        public void Parse_InvalidEndOfExpression_ThrowsException()
        {
            var query = "eq(name,";

            var exception = Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));
            //Assert.Contains("Unexpected end of expression", exception.Message);
        }

        [Fact]
        public void Parse_InvalidCommaAtStart_ThrowsException()
        {
            var query = "eq(,name, \"John\")";

            var exception = Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));
            //Assert.Contains("Unexpected token", exception.Message);
        }

        [Fact]
        public void Parse_ValidIsNullFunction_ReturnsIsNullFunc()
        {
            var query = "isnull(name)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<IsNullFunc>(result.Expression);
            var isNullFunc = (IsNullFunc)result.Expression;
            Assert.Single(isNullFunc.Arguments);
            Assert.IsType<Field>(isNullFunc.Arguments[0]);
        }

        [Fact]
        public void Parse_IsNullInComplexExpression_ReturnsCorrectTree()
        {
            var query = "and(isnull(name), gt(age, 25))";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<AndFunc>(result.Expression);
            var andFunc = (AndFunc)result.Expression;
            Assert.Equal(2, andFunc.Arguments.Count);
            Assert.IsType<IsNullFunc>(andFunc.Arguments[0]);
            Assert.IsType<GtFunc>(andFunc.Arguments[1]);
        }

        [Fact]
        public void Parse_InvalidIsNullFunction_WrongArgCount_ThrowsException()
        {
            var query = "isnull(name, age)";

            var exception = Assert.Throws<Exception>(() => _parser.Parse(query, _validFields));
        }

        [Fact]
        public void Parse_ByteFieldWithLiteral_ReturnsCorrectExpression()
        {
            var query = "eq(status, 5)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.Equal(2, eqFunc.Arguments.Count);
            Assert.IsType<Field>(eqFunc.Arguments[0]);
            Assert.IsType<Literal>(eqFunc.Arguments[1]);
            Assert.Equal(typeof(byte), ((Field)eqFunc.Arguments[0]).ReturnType);
            Assert.Equal((byte)5, ((Literal)eqFunc.Arguments[1]).Value);
        }

        [Fact]
        public void Parse_ByteFieldWithGtComparison_ReturnsCorrectExpression()
        {
            var query = "gt(status, 10)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<GtFunc>(result.Expression);
            var gtFunc = (GtFunc)result.Expression;
            Assert.Equal(2, gtFunc.Arguments.Count);
            Assert.IsType<Field>(gtFunc.Arguments[0]);
            Assert.IsType<Literal>(gtFunc.Arguments[1]);
            Assert.Equal(typeof(byte), ((Field)gtFunc.Arguments[0]).ReturnType);
            Assert.Equal((byte)10, ((Literal)gtFunc.Arguments[1]).Value);
        }

        [Fact]
        public void Parse_ByteFieldWithAddFunction_ReturnsCorrectExpression()
        {
            var query = "eq(add(status, 5), 15)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.Equal(2, eqFunc.Arguments.Count);
            Assert.IsType<AddFunc>(eqFunc.Arguments[0]);
            var addFunc = (AddFunc)eqFunc.Arguments[0];
            Assert.IsType<Field>(addFunc.Arguments[0]);
            Assert.IsType<Literal>(addFunc.Arguments[1]);
            Assert.Equal(typeof(byte), ((Field)addFunc.Arguments[0]).ReturnType);
        }

        [Fact]
        public void Parse_ByteFieldWithAbsFunction_ReturnsCorrectExpression()
        {
            var query = "eq(abs(status), 5)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.Equal(2, eqFunc.Arguments.Count);
            Assert.IsType<AbsFunc>(eqFunc.Arguments[0]);
            var absFunc = (AbsFunc)eqFunc.Arguments[0];
            Assert.IsType<Field>(absFunc.Arguments[0]);
            Assert.Equal(typeof(byte), ((Field)absFunc.Arguments[0]).ReturnType);
        }

        [Fact]
        public void Parse_ByteFieldWithInFunction_ReturnsCorrectExpression()
        {
            var query = "in(status, 1, 2, 3)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<InFunc>(result.Expression);
            var inFunc = (InFunc)result.Expression;
            Assert.Equal(4, inFunc.Arguments.Count);
            Assert.IsType<Field>(inFunc.Arguments[0]);
            Assert.IsType<Literal>(inFunc.Arguments[1]);
            Assert.IsType<Literal>(inFunc.Arguments[2]);
            Assert.IsType<Literal>(inFunc.Arguments[3]);
            Assert.Equal((byte)1, ((Literal)inFunc.Arguments[1]).Value);
            Assert.Equal((byte)2, ((Literal)inFunc.Arguments[2]).Value);
            Assert.Equal((byte)3, ((Literal)inFunc.Arguments[3]).Value);
        }

        [Fact]
        public void Parse_ByteFieldWithIsNullFunction_ReturnsCorrectExpression()
        {
            var query = "isnull(status)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<IsNullFunc>(result.Expression);
            var isNullFunc = (IsNullFunc)result.Expression;
            Assert.Single(isNullFunc.Arguments);
            Assert.IsType<Field>(isNullFunc.Arguments[0]);
            Assert.Equal(typeof(byte), ((Field)isNullFunc.Arguments[0]).ReturnType);
        }

        [Fact]
        public void Parse_ByteFieldMixedWithIntField_ReturnsCorrectExpression()
        {
            var query = "and(gt(status, 5), lt(age, 30))";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<AndFunc>(result.Expression);
            var andFunc = (AndFunc)result.Expression;
            Assert.Equal(2, andFunc.Arguments.Count);
            Assert.IsType<GtFunc>(andFunc.Arguments[0]);
            Assert.IsType<LtFunc>(andFunc.Arguments[1]);
        }

        [Theory]
        [InlineData("eq(year(dateFrom), 2020)", typeof(YearFunc))]
        [InlineData("eq(month(dateFrom), 1)", typeof(MonthFunc))]
        [InlineData("eq(day(dateFrom), 15)", typeof(DayFunc))]
        [InlineData("eq(dayofyear(dateFrom), 32)", typeof(DayOfYearFunc))]
        [InlineData("eq(hour(dateFrom), 12)", typeof(HourFunc))]
        [InlineData("eq(minute(dateFrom), 30)", typeof(MinuteFunc))]
        [InlineData("eq(second(dateFrom), 0)", typeof(SecondFunc))]
        [InlineData("eq(dayofweek(dateFrom), 0)", typeof(DayOfWeekFunc))]
        public void Parse_ValidDateTimeGetter_ReturnsExpectedFunc(string query, Type expectedFuncType)
        {
            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.IsType(expectedFuncType, eqFunc.Arguments[0]);
        }

        [Fact]
        public void Parse_ValidDateFunction_ReturnsDateFunc()
        {
#if NET6_0_OR_GREATER
            var query = "eq(date(dateFrom), \"2020-01-01\")";
#else
            var query = "eq(date(dateFrom), dateTo)";
#endif

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.IsType<DateFunc>(eqFunc.Arguments[0]);
        }

        [Fact]
        public void Parse_YearQuotedDateLiteral_ReturnsYearFunc()
        {
            var query = "eq(year(\"2020-01-15\"), 2020)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.IsType<YearFunc>(eqFunc.Arguments[0]);
            var yearFunc = (YearFunc)eqFunc.Arguments[0];
            Assert.IsType<Literal>(yearFunc.Arguments[0]);
            Assert.Equal(typeof(DateTime), yearFunc.Arguments[0].ReturnType);
        }

        [Theory]
        [InlineData("gt(addyears(dateFrom, 1), dateTo)", typeof(AddYearsFunc), 1)]
        [InlineData("gt(addmonths(dateFrom, 0), dateTo)", typeof(AddMonthsFunc), 0)]
        [InlineData("gt(adddays(dateFrom, -7), dateTo)", typeof(AddDaysFunc), -7)]
        [InlineData("gt(addhours(dateFrom, 24), dateTo)", typeof(AddHoursFunc), 24)]
        [InlineData("gt(addminutes(dateFrom, -30), dateTo)", typeof(AddMinutesFunc), -30)]
        [InlineData("gt(addseconds(dateFrom, 0), dateTo)", typeof(AddSecondsFunc), 0)]
        public void Parse_ValidDateTimeAdd_ReturnsExpectedFunc(string query, Type expectedFuncType, int expectedAmount)
        {
            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<GtFunc>(result.Expression);
            var gtFunc = (GtFunc)result.Expression;
            Assert.IsType(expectedFuncType, gtFunc.Arguments[0]);
            var addFunc = (AbstractFunction)gtFunc.Arguments[0];
            Assert.IsType<Literal>(addFunc.Arguments[1]);
            Assert.Equal(expectedAmount, ((Literal)addFunc.Arguments[1]).Value);
        }

        [Fact]
        public void Parse_NestedYearOfAddDays_ReturnsYearFunc()
        {
            var query = "eq(year(adddays(dateFrom, 1)), 2020)";

            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.IsType<YearFunc>(eqFunc.Arguments[0]);
            var yearFunc = (YearFunc)eqFunc.Arguments[0];
            Assert.IsType<AddDaysFunc>(yearFunc.Arguments[0]);
        }

        [Fact]
        public void Parse_AddDaysOnStringField_Throws()
        {
            var query = "eq(adddays(name, 1), dateTo)";

            Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));
        }

        [Fact]
        public void Parse_AddDaysFractionalAmount_Throws()
        {
            var query = "gt(adddays(dateFrom, 1.5), dateTo)";

            Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));
        }

        [Fact]
        public void Parse_YearOnIntField_Throws()
        {
            var query = "eq(year(age), 2020)";

            Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));
        }

        [Fact]
        public void Parse_InvalidYearArity_ThrowsException()
        {
            var query = "eq(year(), 2020)";

            Assert.ThrowsAny<Exception>(() => _parser.Parse(query, _validFields));
        }

        [Fact]
        public void Parse_InvalidYearExtraArgs_ThrowsException()
        {
            var query = "eq(year(dateFrom, dateTo), 2020)";

            var exception = Assert.Throws<Exception>(() => _parser.Parse(query, _validFields));
            Assert.Contains("Year() function should have 1 argument", exception.Message);
        }

        [Fact]
        public void Parse_InvalidAddDaysMissingArgs_ThrowsException()
        {
            var query = "gt(adddays(dateFrom), dateTo)";

            var exception = Assert.Throws<Exception>(() => _parser.Parse(query, _validFields));
            Assert.Contains("Adddays() function should have 2 arguments", exception.Message);
        }

        [Theory]
        [InlineData("eq(mod(age, 2), 0)", typeof(ModFunc))]
        [InlineData("eq(floor(doubleField), 1.5)", typeof(FloorFunc))]
        [InlineData("eq(ceiling(doubleField), 2.0)", typeof(CeilingFunc))]
        [InlineData("eq(ceil(doubleField), 2.0)", typeof(CeilingFunc))]
        [InlineData("eq(round(doubleField), 2.0)", typeof(RoundFunc))]
        [InlineData("eq(round(doubleField, -1), 20.0)", typeof(RoundFunc))]
        [InlineData("eq(sign(age), 1)", typeof(SignFunc))]
        [InlineData("eq(power(age, 2), 25)", typeof(PowerFunc))]
        [InlineData("eq(pow(age, 2), 25)", typeof(PowerFunc))]
        [InlineData("eq(sqrt(age), 5)", typeof(SqrtFunc))]
        [InlineData("eq(min(age, 18), 18)", typeof(MinFunc))]
        [InlineData("eq(max(age, 65), 65)", typeof(MaxFunc))]
        public void Parse_ValidNumericFunction_ReturnsExpectedFunc(string query, Type expectedFuncType)
        {
            var result = _parser.Parse(query, _validFields);

            Assert.NotNull(result);
            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.IsType(expectedFuncType, eqFunc.Arguments[0]);
        }

        [Fact]
        public void Parse_RoundWithNegativeDigits_ReturnsRoundFunc()
        {
            var query = "eq(round(age, -1), 30)";

            var result = _parser.Parse(query, _validFields);

            Assert.IsType<RoundFunc>(((EqFunc)result.Expression).Arguments[0]);
            var roundFunc = (RoundFunc)((EqFunc)result.Expression).Arguments[0];
            Assert.Equal(2, roundFunc.Arguments.Count);
            Assert.IsType<Literal>(roundFunc.Arguments[1]);
            Assert.Equal(-1, ((Literal)roundFunc.Arguments[1]).Value);
        }

        [Fact]
        public void Parse_ModOnStringField_Throws()
        {
            var query = "eq(mod(name, 2), 0)";

            Assert.ThrowsAny<Exception>(() => _parser.Parse(query, _validFields));
        }

        [Fact]
        public void Parse_RoundWithDoubleDigits_Throws()
        {
            var query = "eq(round(age, 1.5), 30)";

            Assert.Throws<ArgumentException>(() => _parser.Parse(query, _validFields));
        }

        [Fact]
        public void Parse_InvalidModArity_ThrowsException()
        {
            var query = "eq(mod(age), 0)";

            var exception = Assert.Throws<Exception>(() => _parser.Parse(query, _validFields));
            Assert.Contains("Mod() function should have 2 arguments", exception.Message);
        }

        [Fact]
        public void Parse_InvalidRoundArity_ThrowsException()
        {
            var query = "eq(round(age, 1, 2), 30)";

            var exception = Assert.Throws<Exception>(() => _parser.Parse(query, _validFields));
            Assert.Contains("Round() function should have 1 or 2 arguments", exception.Message);
        }
    }
}