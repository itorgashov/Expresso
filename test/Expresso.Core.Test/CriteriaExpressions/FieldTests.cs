using Expresso.Core.CriteriaExpressions;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class FieldTests
    {
        [Theory]
        [InlineData("FieldName", typeof(int))]
        [InlineData("AnotherField", typeof(string))]
        [InlineData("DateField", typeof(DateTime))]
        public void Field_ValidParameters_SetsProperties(string name, Type type)
        {
            var field = new Field(name, type);

            Assert.Equal(name, field.Name);
            Assert.Equal(type, field.ReturnType);
        }

        [Fact]
        public void Field_NullName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Field(null!, typeof(int)));
        }

        [Fact]
        public void Field_NullType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Field("FieldName", null!));
        }

        [Fact]
        public void Equals_ReturnsTrue_ForSameNameAndType()
        {
            Field field1 = new("Name", typeof(string));
            Field field2 = new("Name", typeof(string));

            Assert.True(field1.Equals(field2));
            Assert.True(field1 == field2);
            Assert.False(field1 != field2);
        }

        [Fact]
        public void Equals_ReturnsFalse_ForDifferentName()
        {
            Field field1 = new("Name", typeof(string));
            Field field2 = new("OtherName", typeof(string));

            Assert.False(field1.Equals(field2));
            Assert.False(field1 == field2);
            Assert.True(field1 != field2);
        }

        [Fact]
        public void Equals_ReturnsFalse_ForDifferentType()
        {
            Field field1 = new("Name", typeof(string));
            Field field2 = new("Name", typeof(int));

            Assert.False(field1.Equals(field2));
            Assert.False(field1 == field2);
            Assert.True(field1 != field2);
        }

        [Fact]
        public void GetHashCode_Matches_ForEqualFields()
        {
            Field field1 = new("Name", typeof(string));
            Field field2 = new("Name", typeof(string));

            Assert.Equal(field1.GetHashCode(), field2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_Differs_ForDifferentFields()
        {
            Field field1 = new("Name1", typeof(string));
            Field field2 = new("Name2", typeof(string));

            Assert.NotEqual(field1.GetHashCode(), field2.GetHashCode());
        }

        [Fact]
        public void Equals_ReturnsFalse_ForDifferentScope()
        {
            Field field1 = new("Name", typeof(string), "authors");
            Field field2 = new("Name", typeof(string));

            Assert.False(field1.Equals(field2));
            Assert.Equal("authors", field1.Scope);
            Assert.Null(field2.Scope);
        }
    }
}
