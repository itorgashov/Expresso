using Expresso.Core.Paging;

namespace Expresso.Tests.Core.Paging
{
    public class PagingDirectiveTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_RejectsPageBelowOne(int page)
        {
            var error = Assert.Throws<ArgumentOutOfRangeException>(() => new PagingDirective(page: page, pageSize: 10));
            Assert.Equal("page", error.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public void Constructor_RejectsPageSizeBelowOne(int pageSize)
        {
            var error = Assert.Throws<ArgumentOutOfRangeException>(() => new PagingDirective(page: 1, pageSize: pageSize));
            Assert.Equal("pageSize", error.ParamName);
        }

        [Fact]
        public void Constructor_RejectsNegativeSkip()
        {
            var error = Assert.Throws<ArgumentOutOfRangeException>(() => new PagingDirective(skip: -1));
            Assert.Equal("skip", error.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public void Constructor_RejectsTakeBelowOne(int take)
        {
            var error = Assert.Throws<ArgumentOutOfRangeException>(() => new PagingDirective(take: take));
            Assert.Equal("take", error.ParamName);
        }

        [Fact]
        public void Constructor_RejectsOffsetPastInt32()
        {
            var error = Assert.Throws<ArgumentOutOfRangeException>(() => new PagingDirective(page: 3, pageSize: int.MaxValue));
            Assert.Equal("page", error.ParamName);
        }

        [Fact]
        public void Constructor_AllowsOffsetAtInt32Max()
        {
            var paging = new PagingDirective(page: 2, pageSize: int.MaxValue);
            Assert.Equal(int.MaxValue, paging.Offset);
        }

        [Fact]
        public void Constructor_ValidatesIgnoredValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PagingDirective(page: 2, pageSize: 10, skip: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PagingDirective(page: 0, skip: 1, take: 2));
        }

        [Fact]
        public void PageSizeAlone_IsFirstPage()
        {
            var paging = new PagingDirective(pageSize: 25);
            Assert.True(paging.IsPageBased);
            Assert.Equal(0, paging.Offset);
            Assert.Equal(25, paging.Limit);
            Assert.False(paging.IsEmpty);
        }

        [Fact]
        public void PageAlone_IsIgnored()
        {
            var paging = new PagingDirective(page: 4);
            Assert.False(paging.IsPageBased);
            Assert.Equal(4, paging.Page);
            Assert.Null(paging.PageSize);
            Assert.Equal(0, paging.Offset);
            Assert.Null(paging.Limit);
            Assert.True(paging.IsEmpty);
            Assert.False(paging.HasPageAndSkipTake);
        }

        [Fact]
        public void PageWithoutSize_StillAppliesSkipTake()
        {
            var paging = new PagingDirective(page: 3, skip: 1, take: 2);
            Assert.False(paging.IsPageBased);
            Assert.True(paging.HasPageAndSkipTake);
            Assert.Equal(1, paging.Offset);
            Assert.Equal(2, paging.Limit);
        }

        [Fact]
        public void PageWinsOverSkipTake()
        {
            var paging = new PagingDirective(page: 2, pageSize: 10, skip: 0, take: 5);
            Assert.True(paging.IsPageBased);
            Assert.True(paging.HasPageAndSkipTake);
            Assert.Equal(10, paging.Offset);
            Assert.Equal(10, paging.Limit);
            Assert.Equal(0, paging.Skip);
            Assert.Equal(5, paging.Take);
        }

        [Fact]
        public void None_AndSkipZero_AreEmpty()
        {
            Assert.True(PagingDirective.None.IsEmpty);
            Assert.Equal(0, PagingDirective.None.Offset);
            Assert.Null(PagingDirective.None.Limit);

            var skipZero = new PagingDirective(skip: 0);
            Assert.True(skipZero.IsEmpty);
            Assert.False(skipZero.HasPageAndSkipTake);
        }

        [Fact]
        public void SkipAndTake_SetOffsetAndLimit()
        {
            var paging = new PagingDirective(skip: 4, take: 2);
            Assert.False(paging.IsPageBased);
            Assert.Equal(4, paging.Offset);
            Assert.Equal(2, paging.Limit);
            Assert.False(paging.IsEmpty);
        }

        [Theory]
        [InlineData(0, 10, 0)]
        [InlineData(20, 10, 2)]
        [InlineData(21, 10, 3)]
        [InlineData(1, 1, 1)]
        public void TotalPages_RoundsUp(long total, int pageSize, long expected)
        {
            Assert.Equal(expected, PagingDirective.TotalPages(total, pageSize));
        }

        [Fact]
        public void TotalPages_RejectsInvalidArguments()
        {
            Assert.Equal("totalCount", Assert.Throws<ArgumentOutOfRangeException>(() => PagingDirective.TotalPages(-1, 10)).ParamName);
            Assert.Equal("pageSize", Assert.Throws<ArgumentOutOfRangeException>(() => PagingDirective.TotalPages(1, 0)).ParamName);
        }
    }
}
