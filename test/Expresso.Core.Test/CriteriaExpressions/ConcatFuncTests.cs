using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class ConcatFuncTests
    {
        [Fact]
        public void ConcatFunc_ValidArguments_SetsReturnType()
        {
            var args = new List<AbstractExpression>
            {
                new MockExpressionOfType(typeof(string)),
                new MockExpressionOfType(typeof(string)),
                new MockExpressionOfType(typeof(string))
            };

            var func = new ConcatFunc(args);

            Assert.Equal(3, func.Arguments.Count);
            Assert.Equal(typeof(string), func.ReturnType);
        }

        [Fact]
        public void ConcatFunc_FewerThanTwoArguments_ThrowsArgumentException()
        {
            var args = new List<AbstractExpression> { new MockExpressionOfType(typeof(string)) };
            Assert.Throws<ArgumentException>(() => new ConcatFunc(args));
        }

        [Fact]
        public void ConcatFunc_NullList_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ConcatFunc(null!));
        }

        [Fact]
        public void ConcatFunc_NonStringArgument_ThrowsArgumentException()
        {
            var args = new List<AbstractExpression>
            {
                new MockExpressionOfType(typeof(string)),
                new MockExpressionOfType(typeof(int))
            };
            Assert.Throws<ArgumentException>(() => new ConcatFunc(args));
        }
    }
}
