using Expresso.Core.Paging;

namespace Expresso.Rendering.Linq.Test
{
    /// <summary>
    /// Scenario: a caller windows an already ordered query.
    /// Setup: an in-memory queryable of three ids and a <see cref="PagingDirective"/>.
    /// Expect: Skip is omitted at offset 0, counts are <see cref="ParameterBox{T}"/> reads, and an empty directive returns the same query.
    /// </summary>
    public class PagingTests
    {
        [Fact]
        public void Page_Queryable_BoxesSkipAndTake()
        {
            var source = new[] { 1, 2, 3 }.AsQueryable();
            var paged = source.Page(new PagingDirective(page: 2, pageSize: 1));

            var text = paged.Expression.ToString();
            Assert.Contains(".Skip(", text);
            Assert.Contains(".Take(", text);
            Assert.Contains("ParameterBox`1[System.Int32]).Value", text);
            Assert.Equal(new[] { 2 }, paged.ToArray());
        }

        [Fact]
        public void Page_Queryable_OmitsSkipAtOffsetZero()
        {
            var source = new[] { 1, 2, 3 }.AsQueryable();
            var paged = source.Page(new PagingDirective(take: 2));

            Assert.DoesNotContain(".Skip(", paged.Expression.ToString());
            Assert.Contains(".Take(", paged.Expression.ToString());
            Assert.Equal(new[] { 1, 2 }, paged.ToArray());
        }

        [Fact]
        public void Page_Empty_ReturnsSameInstance()
        {
            var source = new[] { 1, 2, 3 }.AsQueryable();
            Assert.Same(source, source.Page(PagingDirective.None));
            Assert.Same(source, source.Page(new PagingDirective(page: 3)));

            IEnumerable<int> rows = new[] { 1, 2, 3 };
            Assert.Same(rows, rows.Page(PagingDirective.None));
        }

        [Fact]
        public void Page_RejectsNull()
        {
            IQueryable<int> query = new[] { 1 }.AsQueryable();
            Assert.Throws<ArgumentNullException>(() => LinqQueryExtensions.Page(query, null!));
            Assert.Throws<ArgumentNullException>(() => LinqQueryExtensions.Page<int>(null!, PagingDirective.None));

            IEnumerable<int> rows = new[] { 1 };
            Assert.Throws<ArgumentNullException>(() => LinqQueryExtensions.Page(rows, null!));
            Assert.Throws<ArgumentNullException>(() => LinqQueryExtensions.Page<int>(null!, PagingDirective.None));
        }

        [Fact]
        public void Page_Enumerable_SkipsAndTakes()
        {
            var ids = new[] { 1, 2, 3, 4, 5, 6 }.Page(new PagingDirective(skip: 2, take: 3));
            Assert.Equal(new[] { 3, 4, 5 }, ids);
        }
    }
}
