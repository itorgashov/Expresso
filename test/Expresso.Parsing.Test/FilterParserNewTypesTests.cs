using Expresso.Core.CriteriaExpressions;
using Expresso.Parsing;

namespace Expresso.Tests.Parsing
{
    public class FilterParserNewTypesTests
    {
        private readonly FilterParser _parser = new();
        private static readonly Guid SampleGuid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

        private readonly (string, Type)[] _validFields =
        [
            ("id", typeof(Guid)),
            ("dateFrom", typeof(DateTime)),
        ];

        [Fact]
        public void Parse_EqGuidLiteral_ReturnsEqFunc()
        {
            var query = $"eq(id, \"{SampleGuid}\")";

            var result = _parser.Parse(query, _validFields);

            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.IsType<Literal>(eqFunc.Arguments[1]);
            Assert.Equal(SampleGuid, ((Literal)eqFunc.Arguments[1]).Value);
        }

        [Fact]
        public void Parse_InGuid_ReturnsInFunc()
        {
            var query = $"in(id, \"{SampleGuid}\", \"{Guid.Empty}\")";

            var result = _parser.Parse(query, _validFields);

            Assert.IsType<InFunc>(result.Expression);
        }

        [Fact]
        public void Parse_IsNullGuid_ReturnsIsNullFunc()
        {
            var result = _parser.Parse("isnull(id)", _validFields);
            Assert.IsType<IsNullFunc>(result.Expression);
        }

        [Fact]
        public void Parse_GtGuid_Throws()
        {
            Assert.ThrowsAny<Exception>(() => _parser.Parse($"gt(id, \"{SampleGuid}\")", _validFields));
        }

#if NET6_0_OR_GREATER
        private readonly (string, Type)[] _dateOnlyFields =
        [
            ("born", typeof(DateOnly)),
            ("starts", typeof(TimeOnly)),
            ("dateFrom", typeof(DateTime)),
        ];

        [Fact]
        public void Parse_EqDateOnlyLiteral_ReturnsEqFunc()
        {
            var result = _parser.Parse("eq(born, \"2020-01-15\")", _dateOnlyFields);
            Assert.IsType<EqFunc>(result.Expression);
            var eqFunc = (EqFunc)result.Expression;
            Assert.IsType<Literal>(eqFunc.Arguments[1]);
            Assert.Equal(typeof(DateOnly), eqFunc.Arguments[1].ReturnType);
        }

        [Fact]
        public void Parse_AddDaysOnDateOnly_ReturnsDateOnlyAdd()
        {
            var result = _parser.Parse("gt(adddays(born, 1), born)", _dateOnlyFields);
            var gtFunc = (GtFunc)result.Expression!;
            var addDays = (AddDaysFunc)gtFunc.Arguments[0];
            Assert.Equal(typeof(DateOnly), addDays.ReturnType);
        }

        [Fact]
        public void Parse_DateComparedToDateTimeField_Throws()
        {
            Assert.Throws<ArgumentException>(() => _parser.Parse("eq(date(dateFrom), dateFrom)", _dateOnlyFields));
        }

        [Fact]
        public void Parse_TimeFunction_ReturnsTimeFunc()
        {
            var result = _parser.Parse("eq(time(dateFrom), \"14:30:00\")", _dateOnlyFields);
            var eqFunc = (EqFunc)result.Expression!;
            Assert.IsType<TimeFunc>(eqFunc.Arguments[0]);
        }

        [Fact]
        public void Parse_AddHoursOnDateOnly_Throws()
        {
            Assert.Throws<ArgumentException>(() => _parser.Parse("eq(addhours(born, 1), born)", _dateOnlyFields));
        }

        [Fact]
        public void Parse_AddDaysOnTimeOnly_Throws()
        {
            Assert.Throws<ArgumentException>(() => _parser.Parse("eq(adddays(starts, 1), starts)", _dateOnlyFields));
        }
#endif
    }
}
