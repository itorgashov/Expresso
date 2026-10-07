using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.EntityFramework.Test
{
    public class OraclePowerPromotionTests
    {
        [Theory]
        [MemberData(nameof(IntegerOperands))]
        public void IntegerPromotion_ThrowsTheDocumentedGap(AbstractExpression value, AbstractExpression exponent)
        {
            // Scenario: EF6 Oracle casts integer operands to BINARY_DOUBLE even through a NUMBER identity function.
            // The exact-subset profile must reject the expression before a query can silently change rows or sort order.
            var transformer = new Ef6ExpressionToLinqTransformer(Ef6Provider.Oracle);
            var filter = new FilterCriteria { Expression = new IsNullFunc(new PowerFunc(value, exponent)) };
            var exception = Assert.Throws<NotSupportedException>(() => transformer.BuildPredicate(filter, WidgetLinqMapping.Create()));
            Assert.Contains("'power'", exception.Message);
            Assert.Contains("integer-to-double promotion changes Oracle NUMBER arithmetic to BINARY_DOUBLE", exception.Message);
        }

        [Fact]
        public void DoubleOnlyOperands_StillBuild()
        {
            // Scenario: double binds are NUMBER on EF6, while actual binary-double columns retain their stored type.
            var transformer = new Ef6ExpressionToLinqTransformer(Ef6Provider.Oracle);
            foreach (var value in new AbstractExpression[] { new Literal(2.0), new Field("amount", typeof(double)) })
            {
                var filter = new FilterCriteria { Expression = new IsNullFunc(new PowerFunc(value, new Literal(0.5))) };
                Assert.NotNull(transformer.BuildPredicate(filter, WidgetLinqMapping.Create()));
            }
        }

        public static IEnumerable<object[]> IntegerOperands()
        {
            yield return new object[] { new Field("age", typeof(int)), new Literal(1.0) };
            yield return new object[] { new Field("code", typeof(byte)), new Literal(0.5) };
            yield return new object[] { new Literal(0.5), new AddFunc(new Field("age", typeof(int)), new Literal(1023.0)) };
            yield return new object[] { new Literal(2.0), new DivFunc(new Field("age", typeof(int)), new Literal(2.0)) };
            yield return new object[] { new Literal(2.0), new Literal(1024) };
        }
    }
}
