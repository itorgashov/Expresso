using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class CommonExpressionTests
    {
        [Fact]
        public void Equals_ReturnsFalse_ForDifferentExpressionTypes()
        {
            Field field = new("Test", typeof(string));
            Literal literal = new("value");

            Assert.False(field.Equals(literal));
            Assert.False(field == literal);
            Assert.True(field != literal);
        }

        [Fact]
        public void Equals_ReturnsFalse_ForNull()
        {
            Field field = new("Test", typeof(string));

            Assert.False(field.Equals(null));
            Assert.False(field == null);
            Assert.False(null == field);
            Assert.True(field != null);
            Assert.True(null != field);
        }

        [Fact]
        public void OperatorEquals_HandlesNullBothSides()
        {
            AbstractExpression nullExpr = null!;

            Assert.True(nullExpr == null);
            Assert.False(nullExpr != null);
        }
    }
}
