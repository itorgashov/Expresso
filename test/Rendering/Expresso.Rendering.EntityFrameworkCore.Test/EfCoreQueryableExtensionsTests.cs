using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Sorting;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.EntityFrameworkCore.Test
{
    /// <summary><c>IncludeSorted</c>: filtered includes ordered by nested <c>sortfor</c> directives (SQLite golden SQL).</summary>
    public class EfCoreQueryableExtensionsTests
    {
        private static readonly Field Label = new("label", typeof(string), "tags");
        private static readonly Field Score = new("score", typeof(int), "tags");
        private static readonly Field Kind = new("kind", typeof(string), "tag_meta");

        [Fact]
        public void IncludeSorted_TagsDirective_OrdersIncludedTags()
        {
            using var context = TestWidgetContext.Sqlite();

            var sql = context.IncludeSql(Nested(("tags", Sort((Label, SortDirection.Ascending), (Score, SortDirection.Descending)))));

            Assert.Contains("LEFT JOIN \"widget_tag\" AS \"w0\" ON \"w\".\"Id\" = \"w0\".\"WidgetId\"", sql);
            Assert.EndsWith("ORDER BY \"w\".\"Id\", \"w0\".\"Label\", \"w0\".\"Score\" DESC", sql);
        }

        [Fact]
        public void IncludeSorted_DeeperDirective_ThenIncludesOrderedMeta()
        {
            // Scenario: sortfor tags (label asc) containing sortfor tag_meta (kind desc).
            // Expect Include(tags ordered).ThenInclude(tag_meta ordered): one join chain, meta ordered inside each tag.
            using var context = TestWidgetContext.Sqlite();
            var tags = new SortDirective(Sort((Label, SortDirection.Ascending)).Items, new[] { new CollectionSort("tag_meta", Sort((Kind, SortDirection.Descending))) });

            var sql = context.IncludeSql(Nested(("tags", tags)));

            Assert.Contains("LEFT JOIN \"widget_tag_meta\" AS \"w1\" ON \"w0\".\"Id\" = \"w1\".\"TagId\"", sql);
            Assert.EndsWith("ORDER BY \"w\".\"Id\", \"t\".\"Label\", \"t\".\"Id\", \"t\".\"Kind\" DESC", sql);
        }

        [Fact]
        public void IncludeSorted_DirectiveWithOnlyNested_IncludesWithoutOrdering()
        {
            using var context = TestWidgetContext.Sqlite();
            var tags = new SortDirective(Array.Empty<SortDirectiveItem>(), new[] { new CollectionSort("tag_meta", Sort((Kind, SortDirection.Ascending))) });

            var sql = context.IncludeSql(Nested(("tags", tags)));

            Assert.EndsWith("ORDER BY \"w\".\"Id\", \"t\".\"Id\", \"t\".\"Kind\"", sql);
        }

        [Fact]
        public void IncludeSorted_ParentItems_AreNotApplied()
        {
            using var context = TestWidgetContext.Sqlite();
            var root = new SortDirective(
                new[] { new SortDirectiveItem { Expression = new Field("name", typeof(string)), Direction = SortDirection.Descending } },
                new[] { new CollectionSort("tags", Sort((Label, SortDirection.Ascending))) });

            var sql = context.IncludeSql(root);

            Assert.EndsWith("ORDER BY \"w\".\"Id\", \"w0\".\"Label\"", sql);
        }

        [Fact]
        public void IncludeSorted_NoNested_ReturnsSourceUnchanged()
        {
            using var context = TestWidgetContext.Sqlite();
            var transformer = new EfCoreExpressionToLinqTransformer(context.Database.ProviderName);
            var source = context.Widgets.Where(w => w.Id > 0);

            Assert.Same(source, source.IncludeSorted(transformer, Sort((new Field("name", typeof(string)), SortDirection.Ascending)), WidgetLinqMapping.Create()));
        }

        [Fact]
        public void IncludeSorted_UnmappedCollection_Throws()
        {
            using var context = TestWidgetContext.Sqlite();

            var ex = Assert.Throws<ArgumentException>(() => context.IncludeSql(Nested(("parts", Sort((Label, SortDirection.Ascending))))));

            Assert.Contains("parts", ex.Message);
        }

        [Fact]
        public void IncludeSorted_ComputedNavigation_ThrowsWithGuidance()
        {
            using var context = TestWidgetContext.Sqlite();
            var mapping = new LinqQueryMapping<Widget>().Collection("tags", w => w.Tags.Where(t => t.Score > 0), WidgetLinqMapping.Tags());

            var ex = Assert.Throws<NotSupportedException>(() => context.IncludeSql(Nested(("tags", Sort((Label, SortDirection.Ascending)))), mapping));

            Assert.Contains("OrderBy", ex.Message);
        }

        [Fact]
        public void IncludeSorted_NullArguments_Throw()
        {
            using var context = TestWidgetContext.Sqlite();
            var transformer = new EfCoreExpressionToLinqTransformer(EfCoreProvider.Sqlite);
            var sort = Nested(("tags", Sort((Label, SortDirection.Ascending))));
            var mapping = WidgetLinqMapping.Create();

            Assert.Throws<ArgumentNullException>(() => ((IQueryable<Widget>)null!).IncludeSorted(transformer, sort, mapping));
            Assert.Throws<ArgumentNullException>(() => context.Widgets.IncludeSorted(null!, sort, mapping));
            Assert.Throws<ArgumentNullException>(() => context.Widgets.IncludeSorted(transformer, null!, mapping));
            Assert.Throws<ArgumentNullException>(() => context.Widgets.IncludeSorted(transformer, sort, null!));
        }

        private static SortDirective Sort(params (Field Field, SortDirection Direction)[] items) =>
            new(items.Select(i => new SortDirectiveItem { Expression = i.Field, Direction = i.Direction }).ToList());

        private static SortDirective Nested(params (string Name, SortDirective Directive)[] nested) =>
            new(Array.Empty<SortDirectiveItem>(), nested.Select(n => new CollectionSort(n.Name, n.Directive)).ToList());
    }
}
