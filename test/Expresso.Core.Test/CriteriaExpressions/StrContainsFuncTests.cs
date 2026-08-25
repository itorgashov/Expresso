using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class StrContainsFuncTests
    {
        [Fact]
        public void StrContainsFunc_ValidArguments_SetsArguments()
        {
            var field = new Field("TestField", typeof(string));
            var str = new MockExpressionOfType(typeof(string));

            var func = new StrContainsFunc(field, str);

            Assert.Equal(2, func.Arguments.Count);
            Assert.Equal(typeof(bool), func.ReturnType);
        }

        [Fact]
        public void StrContainsFunc_NullField_ThrowsArgumentNullException()
        {
            var str = new MockExpressionOfType(typeof(string));
            Assert.Throws<ArgumentNullException>(() => new StrContainsFunc(null!, str));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(bool))]
        public void StrContainsFunc_InvalidFieldType_ThrowsArgumentException(Type invalidType)
        {
            var field = new Field("TestField", invalidType);
            var str = new MockExpressionOfType(typeof(string));
            Assert.Throws<ArgumentException>(() => new StrContainsFunc(field, str));
        }

        [Fact]
        public void StrContainsFunc_InvalidMatchType_ThrowsArgumentException()
        {
            var field = new Field("TestField", typeof(string));
            var str = new MockExpressionOfType(typeof(int));
            Assert.Throws<ArgumentException>(() => new StrContainsFunc(field, str));
        }
    }
}
