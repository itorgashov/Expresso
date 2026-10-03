using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using static Expresso.Rendering.Linq.Test.TestRows;

namespace Expresso.Rendering.Linq.Test
{
    public class TypeRuleTests
    {
        private static List<Row> Rows() => new()
        {
            new Row { Id = 1, Score = 5, Code = 2, Amount = 2.5, Text = "abc", Clock = new TimeSpan(10, 0, 0) },
            new Row { Id = 2, Score = 7, Code = 3, Amount = null, Text = "x" },
        };

        [Fact]
        public void MappedTypeMismatch_Throws_NamingFieldAndTypes()
        {
            var mapping = new LinqQueryMapping<Row>().Field("score", r => (long)r.Id);
            var ex = Assert.Throws<ArgumentException>(() => InMemoryT.BuildPredicate(F(Eq(Score, 1)), mapping));
            Assert.Contains("score", ex.Message);
            Assert.Contains(nameof(Int64), ex.Message);
            Assert.Contains(nameof(Int32), ex.Message);
        }

        [Fact]
        public void UnmappedFieldAndCollection_Throw_SameMessagesAsSql()
        {
            var empty = new LinqQueryMapping<Row>();
            Assert.Equal("No mapping for the score field",
                Assert.Throws<ArgumentException>(() => InMemoryT.BuildPredicate(F(Eq(Score, 1)), empty)).Message);
            Assert.Equal("No mapping for the children collection",
                Assert.Throws<ArgumentException>(() => InMemoryT.BuildPredicate(F(new AnyFunc(Children)), empty)).Message);
        }

        [Fact]
        public void NumericPromotion_IntPlusDouble_IsDouble()
        {
            // Scenario: add(score, 1.5) promotes to double, so row 1 (5) gives 6.5 exactly.
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(new AddFunc(Score, L(1.5)), 6.5)));
            Assert.Equal(new[] { 1 }, Ids(Rows(), new AndFunc(new List<AbstractExpression>
            {
                new GtFunc(Score, L(4.5)),
                new LtFunc(Score, L(5.5)),
            })));
        }

        [Fact]
        public void IntDivision_Truncates_AndModUsesInts()
        {
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(new DivFunc(Score, L(2)), 2)));
            Assert.Equal(new[] { 1, 2 }, Ids(Rows(), Eq(new ModFunc(Score, L(2)), 1)));
            Assert.Equal(new[] { 2 }, Ids(Rows(), Eq(new DivFunc(Score, L(2.0)), 3.5)));
        }

        [Fact]
        public void Byte_IsPromotedToInt()
        {
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(Code, (byte)2)));
            Assert.Equal(new[] { 2 }, Ids(Rows(), Eq(new AddFunc(Code, L(1)), 4)));
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(new AbsFunc(Code), 2)));
            Assert.Equal(new[] { 2 }, QueryableIds(Rows(), new GtFunc(Code, L(2.5))));
        }

        [Fact]
        public void NumericFunctions_PropagateNull()
        {
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(new FloorFunc(Amount), 2.0)));
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(new CeilingFunc(Amount), 3.0)));
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(new SignFunc(Amount), 1)));
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(new PowerFunc(Amount, L(2)), 6.25)));
            Assert.Equal(new[] { 1 }, Ids(Rows(), new GtFunc(new SqrtFunc(Amount), L(1.5))));
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(new RoundFunc(Amount), 3.0)));
            Assert.Equal(new[] { 1 }, QueryableIds(Rows(), Eq(new RoundFunc(Amount, L(0)), 2.0)));
            Assert.Equal(new[] { 2 }, Ids(Rows(), new IsNullFunc(new RoundFunc(Amount, L(1)))));
        }

        [Fact]
        public void TimeCast_FromStringLiteral_IsParsedToParameter()
        {
#if NET6_0_OR_GREATER
            var rows = new List<Row> { new() { Id = 1, At = new TimeOnly(10, 0) }, new() { Id = 2 } };
            var filter = new EqFunc(new Field("at", typeof(TimeOnly)), new TimeFunc(L("10:00")));
            Assert.Equal(new[] { 1 }, QueryableIds(rows, filter));
            Assert.Equal(new[] { 1, 2 }, Ids(rows, new EqFunc(new DateFunc(L("2022-07-04")), L(new DateOnly(2022, 7, 4)))));
#else
            Assert.Equal(new[] { 1 }, QueryableIds(Rows(), new EqFunc(Clock, new TimeFunc(L("10:00")))));
#endif
        }

        [Fact]
        public void TimeCast_FromNonLiteralString_IsNotSupported()
        {
            Assert.Throws<NotSupportedException>(() => InMemoryT.BuildPredicate(F(new IsNullFunc(new TimeFunc(Text))), Mapping()));
        }

        [Fact]
        public void BuildPredicate_ValidatesArguments()
        {
            Assert.Throws<ArgumentNullException>(() => InMemoryT.BuildPredicate<Row>(null!, Mapping()));
            Assert.Throws<ArgumentException>(() => InMemoryT.BuildPredicate(new FilterCriteria(), Mapping()));
            Assert.Throws<ArgumentNullException>(() => InMemoryT.BuildPredicate<Row>(F(Eq(Id, 1)), null!));
        }

        [Fact]
        public void BuildSortKeys_ValidatesArguments()
        {
            Assert.Throws<ArgumentNullException>(() => InMemoryT.BuildSortKeys<Row>(null!, Mapping()));
            Assert.Throws<ArgumentException>(() => InMemoryT.BuildSortKeys(new SortDirective(new List<SortDirectiveItem>()), Mapping()));
            Assert.Throws<ArgumentNullException>(() => InMemoryT.BuildSortKeys<Row>(Sort(Id), null!));
            Assert.Throws<ArgumentException>(() => InMemoryT.BuildSortKeys(Sort(new AnyFunc(Children)), Mapping()));
            Assert.Throws<NotSupportedException>(() => InMemoryT.BuildSortKeys(Sort(Children), Mapping()));
        }

        [Fact]
        public void LinqNode_ScalarNonBool_HasNoWhenTrue()
        {
            var node = LinqNode.Scalar(System.Linq.Expressions.Expression.Constant(1), null);
            Assert.Throws<InvalidOperationException>(() => node.WhenTrue);
            Assert.Throws<ArgumentNullException>(() => LinqNode.Scalar(null!, null));
        }
    }
}
