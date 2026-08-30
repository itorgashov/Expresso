using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Tests.Core.Mocks;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class CollectionFuncTests
    {
        private static CollectionRef Authors() => new("authors");

        [Fact]
        public void AnyFunc_WithPredicate_IsBoolean()
        {
            var predicate = new EqFunc(new Field("displayname", typeof(string), "authors"), new Literal("Leo Tolstoy"));

            var func = new AnyFunc(Authors(), predicate);

            Assert.Equal(typeof(bool), func.ReturnType);
            Assert.Equal("authors", func.Collection.Name);
            Assert.Same(predicate, func.Predicate);
        }

        [Fact]
        public void AnyFunc_WithoutPredicate_HasSingleArgument()
        {
            var func = new AnyFunc(Authors());

            Assert.Single(func.Arguments);
            Assert.Null(func.Predicate);
        }

        [Fact]
        public void CollectionCountFunc_ReturnsInt()
        {
            var func = new CollectionCountFunc(Authors());

            Assert.Equal(typeof(int), func.ReturnType);
            Assert.Null(func.Predicate);
        }

        [Fact]
        public void CollectionMinFunc_ReturnsSelectorType()
        {
            var selector = new Field("dateofbirth", typeof(DateTime), "authors");

            var func = new CollectionMinFunc(Authors(), selector);

            Assert.Equal(typeof(DateTime), func.ReturnType);
            Assert.Same(selector, func.Selector);
        }

        [Fact]
        public void CollectionQuantifier_NonBooleanPredicate_ThrowsArgumentException()
        {
            var selector = new Field("displayname", typeof(string), "authors");

            Assert.Throws<ArgumentException>(() => new AnyFunc(Authors(), selector));
        }

        [Fact]
        public void CollectionSumFunc_NonNumericSelector_ThrowsArgumentException()
        {
            var selector = new Field("displayname", typeof(string), "authors");

            var ex = Assert.Throws<ArgumentException>(() => new CollectionSumFunc(Authors(), selector));
            Assert.Equal("selector", ex.ParamName);
        }

        [Fact]
        public void CollectionAvgFunc_NumericSelector_ReturnsDouble()
        {
            var selector = new MockExpressionOfType(typeof(int));

            var func = new CollectionAvgFunc(Authors(), selector);

            Assert.Equal(typeof(double), func.ReturnType);
        }

        [Fact]
        public void CollectionSumFunc_DoubleSelector_ReturnsDouble()
        {
            var selector = new MockExpressionOfType(typeof(double));

            var func = new CollectionSumFunc(Authors(), selector);

            Assert.Equal(typeof(double), func.ReturnType);
        }

        [Fact]
        public void CollectionRef_EmptyName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new CollectionRef(" "));
        }

        [Fact]
        public void CollectionRef_Equals_IncludesScope()
        {
            var left = new CollectionRef("awards", "authors");
            var right = new CollectionRef("awards", "authors");
            var other = new CollectionRef("awards");

            Assert.True(left.Equals(right));
            Assert.False(left.Equals(other));
        }
    }
}
