using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Sorting;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;

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
        public void OrderByKeys_RestoresMaterializedOrder()
        {
            var items = new[] { new Widget { Id = 2 }, new Widget { Id = 1 } };

            var ordered = EfCoreLiftedSort.OrderByKeys(new[] { 1, 2 }, items, w => w.Id);

            Assert.Equal(new[] { 1, 2 }, ordered.Select(w => w.Id));
        }

        [Fact]
        public void IncludeSorted_LiteralAncestorKey_IsSharedAcrossSiblingBranches()
        {
            // Scenario: children ordered by score+1, with sibling a and b both ordered by value.
            // The ancestor OrderBy must be one expression (EF rejects two filters on the same navigation),
            // and both grandchild navigations must appear.
            using var context = new BranchContext();
            var leaves = Sort((new Field("value", typeof(int)), SortDirection.Ascending));
            var children = new SortDirective(
                new[] { new SortDirectiveItem { Expression = new AddFunc(new Field("score", typeof(int), "children"), new Literal(1)), Direction = SortDirection.Ascending } },
                new[] { new CollectionSort("a", leaves), new CollectionSort("b", leaves) });
            var leafMapping = new LinqQueryMapping<BranchLeaf>().Field("value", l => l.Value);
            var mapping = new LinqQueryMapping<BranchRoot>().Collection(
                "children",
                r => r.Children,
                new LinqQueryMapping<BranchChild>()
                    .Field("score", c => c.Score)
                    .Collection("a", c => c.A, leafMapping)
                    .Collection("b", c => c.B, leafMapping));
            var transformer = new EfCoreExpressionToLinqTransformer(context.Database.ProviderName);

            var sql = context.Roots.IncludeSorted(transformer, Nested(("children", children)), mapping).ToQueryString();

            Assert.Contains("Score", sql);
            Assert.Contains("AId", sql);
            Assert.Contains("BId", sql);
        }

        [Fact]
        public void IncludeSorted_SiblingNavigations_DoNotShareOneLambda()
        {
            using var context = new TwinContext();
            var shared = Sort((new Field("value", typeof(int)), SortDirection.Ascending));
            var root = new SortDirective(
                Array.Empty<SortDirectiveItem>(),
                new[] { new CollectionSort("a", shared), new CollectionSort("b", shared) });
            var mapping = new LinqQueryMapping<TwinParent>()
                .Collection("a", p => p.A, new LinqQueryMapping<TwinLeaf>().Field("value", l => l.Value))
                .Collection("b", p => p.B, new LinqQueryMapping<TwinLeaf>().Field("value", l => l.Value));
            var transformer = new EfCoreExpressionToLinqTransformer(context.Database.ProviderName);

            var sql = context.Parents.IncludeSorted(transformer, root, mapping).ToQueryString();

            Assert.Contains("AId", sql);
            Assert.Contains("BId", sql);
        }

        [Fact]
        public void IncludeSorted_SiblingPaths_ReuseSharedDirective()
        {
            var shared = new SortDirective(Sort((Label, SortDirection.Ascending)).Items, Array.Empty<CollectionSort>());
            var root = new SortDirective(
                Array.Empty<SortDirectiveItem>(),
                new[] { new CollectionSort("tags", shared), new CollectionSort("tags", shared) });
            using var context = TestWidgetContext.Sqlite();

            var sql = context.IncludeSql(root);

            Assert.Contains("ORDER BY \"w\".\"Id\", \"w0\".\"Label\"", sql);
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

        private sealed class BranchRoot
        {
            public int Id { get; set; }

            public List<BranchChild> Children { get; set; } = new();
        }

        private sealed class BranchChild
        {
            public int Id { get; set; }

            public int RootId { get; set; }

            public int Score { get; set; }

            public List<BranchLeaf> A { get; set; } = new();

            public List<BranchLeaf> B { get; set; } = new();
        }

        private sealed class BranchLeaf
        {
            public int Id { get; set; }

            public int Value { get; set; }

            public int AId { get; set; }

            public int BId { get; set; }
        }

        private sealed class BranchContext : DbContext
        {
            public BranchContext()
                : base(new DbContextOptionsBuilder<BranchContext>().UseSqlite("Data Source=unused.db").Options)
            {
            }

            public DbSet<BranchRoot> Roots => Set<BranchRoot>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.HasExpressoFunctions(Database.ProviderName);
                modelBuilder.Entity<BranchRoot>(e => e.HasMany(r => r.Children).WithOne().HasForeignKey(c => c.RootId));
                modelBuilder.Entity<BranchChild>(e =>
                {
                    e.HasMany(c => c.A).WithOne().HasForeignKey(l => l.AId);
                    e.HasMany(c => c.B).WithOne().HasForeignKey(l => l.BId);
                });
            }
        }

        private sealed class TwinLeaf
        {
            public int Id { get; set; }

            public int Value { get; set; }

            public int AId { get; set; }

            public int BId { get; set; }
        }

        private sealed class TwinParent
        {
            public int Id { get; set; }

            public List<TwinLeaf> A { get; set; } = new();

            public List<TwinLeaf> B { get; set; } = new();
        }

        private sealed class TwinContext : DbContext
        {
            public TwinContext()
                : base(new DbContextOptionsBuilder<TwinContext>().UseSqlite("Data Source=unused.db").Options)
            {
            }

            public DbSet<TwinParent> Parents => Set<TwinParent>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.HasExpressoFunctions(Database.ProviderName);
                modelBuilder.Entity<TwinParent>(e =>
                {
                    e.HasMany(p => p.A).WithOne().HasForeignKey(l => l.AId);
                    e.HasMany(p => p.B).WithOne().HasForeignKey(l => l.BId);
                });
            }
        }
    }
}
