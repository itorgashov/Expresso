using Expresso.Core.Paging;

namespace Expresso.Rendering.TestCases
{
    public abstract partial class RendererParityTests
    {
        [Fact]
        public void RenderPagingClause_Page_BindsOffsetThenLimit()
        {
            var result = T.RenderPagingClause(new PagingDirective(page: 2, pageSize: 10), "pg");

            Assert.Equal(D.Paging(D.Bind("pg", 0), D.Bind("pg", 1)), result.pagingClause);
            Assert.Equal(10, result.parameters[D.Bind("pg", 0)]);
            Assert.Equal(10, result.parameters[D.Bind("pg", 1)]);
        }

        [Fact]
        public void RenderPagingClause_SkipOnly_OmitsLimit()
        {
            var result = T.RenderPagingClause(new PagingDirective(skip: 4), "pg");

            Assert.Equal(D.Paging(D.Bind("pg", 0), null), result.pagingClause);
            Assert.Equal(4, result.parameters[D.Bind("pg", 0)]);
            Assert.Single(result.parameters);
        }

        [Fact]
        public void RenderPagingClause_TakeOnly_BindsZeroOffset()
        {
            var result = T.RenderPagingClause(new PagingDirective(take: 2), "pg");

            Assert.Equal(D.Paging(D.Bind("pg", 0), D.Bind("pg", 1)), result.pagingClause);
            Assert.Equal(0, result.parameters[D.Bind("pg", 0)]);
            Assert.Equal(2, result.parameters[D.Bind("pg", 1)]);
        }

        [Fact]
        public void RenderPagingClause_Empty_RendersNothing()
        {
            var result = T.RenderPagingClause(PagingDirective.None, "pg");
            Assert.Equal(string.Empty, result.pagingClause);
            Assert.Empty(result.parameters);

            var pageOnly = T.RenderPagingClause(new PagingDirective(page: 3), "pg");
            Assert.Equal(string.Empty, pageOnly.pagingClause);
            Assert.Empty(pageOnly.parameters);
        }

        [Fact]
        public void RenderPagingClause_PageWins()
        {
            var result = T.RenderPagingClause(new PagingDirective(page: 2, pageSize: 2, skip: 0, take: 5), "pg");

            Assert.Equal(D.Paging(D.Bind("pg", 0), D.Bind("pg", 1)), result.pagingClause);
            Assert.Equal(2, result.parameters[D.Bind("pg", 0)]);
            Assert.Equal(2, result.parameters[D.Bind("pg", 1)]);
        }

        [Fact]
        public void RenderPagingClause_RejectsBadPrefixAndNull()
        {
            Assert.Throws<ArgumentException>(() => T.RenderPagingClause(PagingDirective.None, "1bad"));
            Assert.Throws<ArgumentNullException>(() => T.RenderPagingClause(null!, "pg"));
        }
    }
}
