using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Sorting;
using Microsoft.Extensions.DependencyInjection;
using static Expresso.Rendering.Linq.Test.TestRows;

namespace Expresso.Rendering.Linq.Test
{
    public class SortAndExtensionTests
    {
        private static List<Row> Rows() => new()
        {
            new Row { Id = 1, Text = "b", Score = 2, Children = { new Child { Value = 2, Name = "y" }, new Child { Value = 1, Name = "x" } } },
            new Row { Id = 2, Text = null, Score = null },
            new Row { Id = 3, Text = "B", Score = 1 },
            new Row { Id = 4, Text = "a", Score = 2 },
        };

        private static int[] InMemorySorted(SortDirective sort) =>
            Rows().OrderBy(InMemoryT, sort, Mapping()).Select(r => r.Id).ToArray();

        [Fact]
        public void InMemory_NullsLastAscending_FirstDescending()
        {
            Assert.Equal(new[] { 3, 1, 4, 2 }, InMemorySorted(Sort(Score)));
            Assert.Equal(new[] { 2, 1, 4, 3 }, InMemorySorted(Sort(Score, SortDirection.Descending)));
        }

        [Fact]
        public void InMemory_StringsSortOrdinally()
        {
            Assert.Equal(new[] { 3, 4, 1, 2 }, InMemorySorted(Sort(Text)));
        }

        [Fact]
        public void InMemory_ThenBy_AppliesSecondaryKeys()
        {
            var sort = new SortDirective(new List<SortDirectiveItem>
            {
                new() { Expression = Score, Direction = SortDirection.Descending },
                new() { Expression = Id, Direction = SortDirection.Descending },
                new() { Expression = Text, Direction = SortDirection.Ascending },
            });
            Assert.Equal(new[] { 2, 4, 1, 3 }, InMemorySorted(sort));
        }

        [Fact]
        public void BooleanSortKey_IsOneWhenTrue_ZeroOtherwise()
        {
            // Scenario: eq(score,2) is TRUE for rows 1 and 4, unknown for row 2 (so 0, like CASE WHEN ... ELSE 0).
            var sort = new SortDirective(new List<SortDirectiveItem>
            {
                new() { Expression = Eq(Score, 2), Direction = SortDirection.Descending },
                new() { Expression = Id, Direction = SortDirection.Ascending },
            });
            Assert.Equal(new[] { 1, 4, 2, 3 }, InMemorySorted(sort));
            var key = QueryableT.BuildSortKeys(sort, Mapping())[0];
            Assert.Equal(typeof(int), key.Key.ReturnType);
            Assert.Equal(typeof(int?), QueryableT.BuildSortKeys(Sort(Score), Mapping())[0].Key.ReturnType);
        }

        [Fact]
        public void Queryable_OrderBy_ComposesOrderByThenBy()
        {
            var sort = new SortDirective(new List<SortDirectiveItem>
            {
                new() { Expression = Id, Direction = SortDirection.Descending },
                new() { Expression = Text, Direction = SortDirection.Descending },
            });
            var query = Rows().AsQueryable().OrderBy(QueryableT, sort, Mapping());
            Assert.Contains("OrderByDescending", query.Expression.ToString());
            Assert.Contains("ThenByDescending", query.Expression.ToString());
            Assert.Equal(new[] { 4, 3, 2, 1 }, query.Select(r => r.Id));
        }

        [Fact]
        public void ResolveNested_FindsDirectiveByPath()
        {
            var inner = Sort(Name);
            var root = new SortDirective(new[] { new SortDirectiveItem { Expression = Id } }, new[] { new CollectionSort("Children", inner) });
            Assert.Same(inner, root.ResolveNested("CHILDREN"));
            Assert.Null(root.ResolveNested("other"));
            Assert.Null(root.ResolveNested());
            Assert.Null(((SortDirective?)null).ResolveNested("children"));
        }

        [Fact]
        public void OrderByNested_SortsChildrenOrLeavesThemUnchanged()
        {
            var root = new SortDirective(new[] { new SortDirectiveItem { Expression = Id } }, new[] { new CollectionSort("children", Sort(Name)) });
            var children = Rows()[0].Children;

            Assert.Equal(new[] { "x", "y" }, children.OrderByNested(InMemoryT, root, ChildMapping(), "children").Select(c => c.Name));
            Assert.Equal(new[] { "x", "y" }, children.AsQueryable().OrderByNested(QueryableT, root, ChildMapping(), "children").Select(c => c.Name));
            Assert.Same(children, children.OrderByNested(InMemoryT, root, ChildMapping(), "missing"));
            var queryable = children.AsQueryable();
            Assert.Same(queryable, queryable.OrderByNested(QueryableT, null, ChildMapping(), "children"));
        }

        [Fact]
        public void Extensions_RequireTransformer()
        {
            Assert.Throws<ArgumentNullException>(() => Rows().Where(null!, F(Eq(Id, 1)), Mapping()));
            Assert.Throws<ArgumentNullException>(() => Rows().AsQueryable().OrderBy(null!, Sort(Id), Mapping()));
        }

        [Fact]
        public void Mapping_IsCaseInsensitive_AndResolvesCollections()
        {
            var mapping = Mapping();
            Assert.True(mapping.Fields.ContainsKey("TEXT"));
            Assert.Equal(typeof(Row), mapping.EntityType);
            Assert.Equal("children", mapping.ResolveCollection("Children")!.Name);
            Assert.Null(mapping.ResolveCollection("children", "nope"));
            Assert.Null(mapping.ResolveCollection());
            Assert.Equal(new[] { 1 }, Ids(Rows(), Eq(new Field("TEXT", typeof(string)), "b")));
        }

        [Fact]
        public void Mapping_ValidatesArguments()
        {
            var mapping = new LinqQueryMapping<Row>();
            Assert.Throws<ArgumentException>(() => mapping.Field(" ", r => r.Id));
            Assert.Throws<ArgumentNullException>(() => mapping.Field<int>("id", null!));
            Assert.Throws<ArgumentNullException>(() => mapping.Collection<Child>("c", null!, ChildMapping()));
            Assert.Throws<ArgumentNullException>(() => mapping.Collection("c", r => r.Children, null!));
        }

        [Fact]
        public void ServiceRegistration_RegistersBothProfiles()
        {
            using var provider = new ServiceCollection().AddLinqExpressionTransformations().BuildServiceProvider();
            Assert.IsType<QueryableExpressionToLinqTransformer>(provider.GetRequiredService<IExpressionToLinqTransformer>());
            Assert.NotNull(provider.GetRequiredService<InMemoryExpressionToLinqTransformer>());
        }

        [Fact]
        public void SortKey_RequiresKey()
        {
            Assert.Throws<ArgumentNullException>(() => new LinqSortKey(null!, SortDirection.Ascending));
        }
    }
}
