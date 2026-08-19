
using Expresso.Core.CriteriaExpressions;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class StrStartswithFuncTests
    {

        [Fact]
        public void StrStartswithFunc_ValidArguments_SetsArguments()
        {
            var field = new Field("TestField", typeof(string));
            var str = new MockExpressionOfType(typeof(string));

            var func = new StrStartswithFunc(field, str);

            Assert.Equal(2, func.Arguments.Count);
        }

        [Fact]
        public void StrStartswithFunc_NullField_ThrowsArgumentNullException()
        {
            var str = new MockExpressionOfType(typeof(string));

            Assert.Throws<ArgumentNullException>(() => new StrStartswithFunc(null!, str));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(bool))]
        public void StrStartswithFunc_InvalidFieldType_ThrowsArgumentException(Type invalidType)
        {
            var field = new Field("TestField", invalidType);
            var str = new MockExpressionOfType(typeof(string));

            Assert.Throws<ArgumentException>(() => new StrStartswithFunc(field, str));
        }

        [Fact]
        public void StrStartswithFunc_NullStringArgument_ThrowsArgumentNullException()
        {
            var field = new Field("TestField", typeof(string));

            Assert.Throws<ArgumentNullException>(() => new StrStartswithFunc(field, null!));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(bool))]
        public void StrStartswithFunc_InvalidStringArgumentType_ThrowsArgumentException(Type invalidType)
        {
            var field = new Field("TestField", typeof(string));
            var str = new MockExpressionOfType(invalidType);

            Assert.Throws<ArgumentException>(() => new StrStartswithFunc(field, str));
        }
    }
}
