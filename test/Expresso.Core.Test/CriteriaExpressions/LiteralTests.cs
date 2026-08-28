using Expresso.Core.CriteriaExpressions;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class LiteralTests
    {
        [Theory]
        [InlineData((byte)42, typeof(byte))]
        [InlineData(123, typeof(int))]
        [InlineData("Test String", typeof(string))]
        [InlineData(45.67, typeof(double))]
        [InlineData(true, typeof(bool))]
        [InlineData(null, null)]
        public void Literal_ValidValues_SetsValueAndReturnType(object value, Type expectedType)
        {
            if (value == null)
            {
                Assert.Throws<ArgumentNullException>(() => new Literal(value));
            }
            else
            {
                var literal = new Literal(value);

                Assert.Equal(value, literal.Value);
                Assert.Equal(expectedType, literal.ReturnType);
            }
        }

        [Fact]
        public void Literal_GuidValue_SetsReturnType()
        {
            var guid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
            var literal = new Literal(guid);
            Assert.Equal(guid, literal.Value);
            Assert.Equal(typeof(Guid), literal.ReturnType);
        }

        [Fact]
        public void Equals_ReturnsTrue_ForSameValue()
        {
            Literal literal1 = new(42);
            Literal literal2 = new(42);

            Assert.True(literal1.Equals(literal2));
            Assert.True(literal1 == literal2);
            Assert.False(literal1 != literal2);
        }

        [Fact]
        public void Equals_ReturnsFalse_ForDifferentValue()
        {
            Literal literal1 = new(42);
            Literal literal2 = new(100);

            Assert.False(literal1.Equals(literal2));
            Assert.False(literal1 == literal2);
            Assert.True(literal1 != literal2);
        }

        [Fact]
        public void GetHashCode_Matches_ForEqualLiterals()
        {
            Literal literal1 = new("test");
            Literal literal2 = new("test");

            Assert.Equal(literal1.GetHashCode(), literal2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_Differs_ForDifferentLiterals()
        {
            Literal literal1 = new(1);
            Literal literal2 = new(2);

            Assert.NotEqual(literal1.GetHashCode(), literal2.GetHashCode());
        }
    }
}
