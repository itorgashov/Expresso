using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class StrEndswithFuncTests
    {
        [Fact]
        public void StrEndswithFunc_ValidArguments_SetsArguments()
        {
            var field = new Field("TestField", typeof(string));
            var str = new MockExpressionOfType(typeof(string));

            var func = new StrEndswithFunc(field, str);

            Assert.Equal(2, func.Arguments.Count);
            Assert.Equal(typeof(bool), func.ReturnType);
        }

        [Fact]
        public void StrEndswithFunc_NullField_ThrowsArgumentNullException()
        {
            var str = new MockExpressionOfType(typeof(string));
            Assert.Throws<ArgumentNullException>(() => new StrEndswithFunc(null!, str));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(bool))]
        public void StrEndswithFunc_InvalidFieldType_ThrowsArgumentException(Type invalidType)
        {
            var field = new Field("TestField", invalidType);
            var str = new MockExpressionOfType(typeof(string));
            Assert.Throws<ArgumentException>(() => new StrEndswithFunc(field, str));
        }

        [Fact]
        public void StrEndswithFunc_InvalidMatchType_ThrowsArgumentException()
        {
            var field = new Field("TestField", typeof(string));
            var str = new MockExpressionOfType(typeof(int));
            Assert.Throws<ArgumentException>(() => new StrEndswithFunc(field, str));
        }
    }
}
