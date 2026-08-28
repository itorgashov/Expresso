using System.Globalization;
using System.Threading;
using Expresso.Core.CriteriaExpressions;
using Expresso.Parsing;

namespace Expresso.Tests.Parsing
{
    [CollectionDefinition(nameof(CultureSensitiveTests), DisableParallelization = true)]
    public class CultureSensitiveTests
    {
    }

    [Collection(nameof(CultureSensitiveTests))]
    public class DateTimeLiteralParserTests
    {
        private readonly FilterParser _parser = new();
        private readonly (string, Type)[] _validFields =
        [
            ("dateFrom", typeof(DateTime)),
        ];

        [Fact]
        public void Parse_DateTimeIsoLiteral_UsesGregorianCalendarRegardlessOfCurrentCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUICulture = CultureInfo.CurrentUICulture;
            var originalThreadCulture = Thread.CurrentThread.CurrentCulture;
            var originalThreadUICulture = Thread.CurrentThread.CurrentUICulture;
            try
            {
                var thai = CultureInfo.GetCultureInfo("th-TH");
                CultureInfo.CurrentCulture = thai;
                CultureInfo.CurrentUICulture = thai;
                Thread.CurrentThread.CurrentCulture = thai;
                Thread.CurrentThread.CurrentUICulture = thai;

                var result = _parser.Parse("eq(dateFrom, \"2020-01-15\")", _validFields);

                var eqFunc = Assert.IsType<EqFunc>(result.Expression);
                var literal = Assert.IsType<Literal>(eqFunc.Arguments[1]);
                Assert.Equal(new DateTime(2020, 1, 15), literal.Value);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUICulture;
                Thread.CurrentThread.CurrentCulture = originalThreadCulture;
                Thread.CurrentThread.CurrentUICulture = originalThreadUICulture;
            }
        }
    }
}
