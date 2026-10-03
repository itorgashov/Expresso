using Expresso.Core.CriteriaExpressions;
using static Expresso.Rendering.Linq.Test.TestRows;

namespace Expresso.Rendering.Linq.Test
{
    /// <summary>
    /// Collection aggregates follow SQL: NULL items are ignored; MIN/MAX/SUM/AVG of no values is NULL (not 0);
    /// COUNT is never NULL.
    /// </summary>
    public class AggregateTests
    {
        // Setup: row 1 values 1 and 2; row 2 only NULL values; row 3 no children.
        private static List<Row> Rows() => new()
        {
            new Row { Id = 1, Children = { new Child { Value = 1, Name = "b", Small = 1 }, new Child { Value = 2, Name = "B", Small = 2 } } },
            new Row { Id = 2, Children = { new Child { Value = null, Name = "a" }, new Child { Value = null } } },
            new Row { Id = 3 },
        };

        [Fact]
        public void Sum_OfNoValues_IsNull()
        {
            Assert.Equal(new[] { 2, 3 }, Ids(Rows(), new IsNullFunc(new CollectionSumFunc(Children, Value))));
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(new CollectionSumFunc(Children, Value), 3)));
            Assert.Empty(Ids(Rows(), Eq(new CollectionSumFunc(Children, Value), 0)));
            Assert.Equal(new[] { 2, 3 }, QueryableIds(Rows(), new IsNullFunc(new CollectionSumFunc(Children, Value))));
        }

        [Fact]
        public void Sum_OfByteSelector_IsInt_AndNeverNullWhenItemsExist()
        {
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(new CollectionSumFunc(Children, Small), 3)));
            Assert.Equal(new[] { 3 }, Ids(Rows(), new IsNullFunc(new CollectionSumFunc(Children, Small))));
        }

        [Fact]
        public void MinMaxAvg_OfNoValues_AreNull()
        {
            Assert.Equal(new[] { 2, 3 }, Ids(Rows(), new IsNullFunc(new CollectionMinFunc(Children, Value))));
            Assert.Equal(new[] { 2, 3 }, Ids(Rows(), new IsNullFunc(new CollectionMaxFunc(Children, Value))));
            Assert.Equal(new[] { 2, 3 }, Ids(Rows(), new IsNullFunc(new CollectionAvgFunc(Children, Value))));
            Assert.Equal(new[] { 2, 3 }, QueryableIds(Rows(), new IsNullFunc(new CollectionMaxFunc(Children, Value))));
        }

        [Fact]
        public void Avg_OfInts_IsFractional_InMemory()
        {
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(new CollectionAvgFunc(Children, Value), 1.5)));
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(new CollectionAvgFunc(Children, Small), 1.5)));
        }

        [Fact]
        public void MinMax_OfStrings_AreOrdinal_InMemory()
        {
            // Scenario: ordinal order is "B" < "a" < "b", so row 1 min is "B" and max is "b"; row 2 ignores the NULL name.
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(new CollectionMinFunc(Children, Name), "B")));
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(new CollectionMaxFunc(Children, Name), "b")));
            Assert.Equal(new[] { 2 }, Ids(Rows(), Eq(new CollectionMaxFunc(Children, Name), "a")));
        }

        [Fact]
        public void Count_IsNeverNull_AndCountsOnlyTrueItems()
        {
            Assert.Equal(new[] { 3 }, Ids(Rows(), Eq(new CollectionCountFunc(Children), 0)));
            Assert.Equal(new[] { 1, 2 }, Ids(Rows(), Eq(new CollectionCountFunc(Children), 2)));
            Assert.Equal(new[] { 2, 3 }, Ids(Rows(), Eq(new CollectionCountFunc(Children, new GtFunc(Value, L(0))), 0)));
            Assert.Empty(Ids(Rows(), new IsNullFunc(new CollectionCountFunc(Children))));
        }
    }
}
