using System.Text.RegularExpressions;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test
{
    public class OracleNumberParameterTests
    {
        [Theory]
        [MemberData(nameof(ComputedArguments))]
        public void ComposedPower_CastsEachDoubleBeforeArithmetic(AbstractExpression value, AbstractExpression exponent)
        {
            // Scenario: casting a completed BINARY_DOUBLE expression cannot undo its range/domain behavior.
            // Build a real Oracle command without opening it; each numeric bind must be converted before use.
            using var context = TestWidgetContext.Oracle();
            var mapping = WidgetLinqMapping.Create();
            var filter = new FilterCriteria { Expression = new GtFunc(new PowerFunc(value, exponent), new Literal(0.0)) };
            var transformer = new EfCoreExpressionToLinqTransformer(context.Database.ProviderName);
            var query = context.Widgets.Where(transformer, filter, mapping).Select(w => w.Id);
            using var command = query.CreateDbCommand();
            Assert.Contains("POWER", command.CommandText, StringComparison.OrdinalIgnoreCase);
            Assert.NotEmpty(command.Parameters);
            foreach (System.Data.Common.DbParameter parameter in command.Parameters)
            {
                Assert.Matches(
                    @"CAST\(\s*:" + Regex.Escape(parameter.ParameterName.TrimStart(':')) + @"\s+AS\s+NUMBER\s*\)",
                    command.CommandText);
            }
        }

        [Fact]
        public void BinaryDoubleColumn_IsNotConvertedToNumber()
        {
            // Scenario: ADO keeps a BINARY_DOUBLE column's arithmetic in binary double; only parameter leaves convert.
            using var context = TestWidgetContext.Oracle();
            var filter = new FilterCriteria
            {
                Expression = new GtFunc(new PowerFunc(new AddFunc(new Field("amount", typeof(double)), new Literal(1.0)), new Literal(1024.0)), new Literal(0.0)),
            };
            var sql = context.WhereSql(filter);
            Assert.Contains("POWER", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("NUMBER", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotMatch(@"CAST\(\s*""w""\.""Amount""\s+AS\s+NUMBER", sql);
        }

        [Theory]
        [InlineData("age", typeof(int))]
        [InlineData("code", typeof(byte))]
        public void IntegerColumnPromotion_RemainsNumber(string field, Type type)
        {
            // Scenario: CLR double promotion of an integer inside a computed exponent must not introduce BINARY_DOUBLE.
            using var context = TestWidgetContext.Oracle();
            var filter = new FilterCriteria
            {
                Expression = new GtFunc(new PowerFunc(new Literal(0.5), new AddFunc(new Field(field, type), new Literal(1023.0))), new Literal(0.0)),
            };
            var sql = context.WhereSql(filter);
            Assert.Contains("POWER", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BINARY_DOUBLE", sql, StringComparison.OrdinalIgnoreCase);
        }

        public static IEnumerable<object[]> ComputedArguments()
        {
            yield return new object[] { new AddFunc(new Literal(1.0), new Literal(1.0)), new Literal(1024.0) };
            yield return new object[] { new Literal(2.0), new AddFunc(new Literal(1023.0), new Literal(1.0)) };
            yield return new object[] { new MultFunc(new Literal(1.0), new Literal(2.0)), new Literal(1024.0) };
            yield return new object[] { new MultFunc(new Literal(1e100), new Literal(1e100)), new Literal(1.0) };
            yield return new object[] { new DivFunc(new Literal(4.0), new Literal(2.0)), new Literal(1024.0) };
        }
    }
}
