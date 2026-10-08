using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Paging;
using Expresso.Core.Sorting;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test
{
    /// <summary>
    /// Scenario: EF Core translates <c>Page</c> on an ordered widget query.
    /// Setup: a provider context that only prints SQL (<c>ToQueryString</c>), no connection.
    /// Expect: SQL Server uses <c>OFFSET</c>/<c>FETCH</c> (and <c>TOP</c> for take-only); SQLite uses <c>LIMIT</c>/<c>OFFSET</c>; counts stay parameters.
    /// </summary>
    public class EfCorePagingSqlTests
    {
        [Fact]
        public void SqlServer_SkipAndTake_UsesOffsetFetchParameters()
        {
            using var context = TestWidgetContext.SqlServer();
            var sql = context.Widgets.OrderBy(w => w.Id).Page(new PagingDirective(page: 2, pageSize: 2)).Select(w => w.Id).ToQueryString();

            Assert.Contains("OFFSET", sql);
            Assert.Contains("FETCH NEXT", sql);
            Assert.Contains("ROWS ONLY", sql);
            Assert.DoesNotContain("OFFSET 2", sql);
            Assert.Contains("@", sql);
        }

        [Fact]
        public void SqlServer_TakeOnly_UsesTop()
        {
            using var context = TestWidgetContext.SqlServer();
            var sql = context.Widgets.Page(new PagingDirective(take: 2)).Select(w => w.Id).ToQueryString();

            Assert.Contains("TOP", sql);
            Assert.DoesNotContain("TOP(2)", sql);
            Assert.DoesNotContain("TOP (2)", sql);
        }

        [Fact]
        public void Sqlite_SkipAndTake_UsesLimitOffset()
        {
            using var context = TestWidgetContext.Sqlite();
            var sql = context.Widgets.OrderBy(w => w.Id).Page(new PagingDirective(skip: 2, take: 2)).Select(w => w.Id).ToQueryString();

            Assert.Contains("LIMIT", sql);
            Assert.Contains("OFFSET", sql);
            Assert.DoesNotContain("OFFSET 2", sql);
        }

        [Fact]
        public void SqlServer_LiftedKeys_PageOnOuterQuery()
        {
            using var context = TestWidgetContext.SqlServer();
            var transformer = new EfCoreExpressionToLinqTransformer(context.Database.ProviderName);
            var sort = new SortDirective(new[]
            {
                new SortDirectiveItem { Expression = new Field("name", typeof(string)), Direction = SortDirection.Ascending },
            });

            var sql = context.Widgets
                .OrderedKeys(transformer, sort, WidgetLinqMapping.Create(), w => w.Id)
                .Page(new PagingDirective(page: 2, pageSize: 3))
                .ToQueryString();

            Assert.Contains("OFFSET", sql);
            Assert.Contains("FETCH NEXT", sql);
            Assert.Contains("ORDER BY", sql);
        }

        [Fact]
        public void Db2_Offset_IsRejected()
        {
            var source = new[] { 1, 2, 3, 4 }.AsQueryable();
            var error = Assert.Throws<NotSupportedException>(() => source.Page(new PagingDirective(page: 2, pageSize: 2), "IBM.EntityFrameworkCore"));
            Assert.Contains("OFFSET", error.Message);
            Assert.Throws<NotSupportedException>(() => source.Page(new PagingDirective(skip: 4), "IBM.EntityFrameworkCore"));
        }

        [Fact]
        public void Db2_TakeOnly_IsTranslated()
        {
            var ids = new[] { 1, 2, 3 }.AsQueryable().Page(new PagingDirective(take: 2), "IBM.EntityFrameworkCore");
            Assert.Equal(new[] { 1, 2 }, ids);
        }

        [Fact]
        public void SqlServer_ProviderPage_KeepsOffset()
        {
            using var context = TestWidgetContext.SqlServer();
            var sql = context.Widgets.OrderBy(w => w.Id)
                .Page(new PagingDirective(page: 2, pageSize: 2), context.Database.ProviderName)
                .Select(w => w.Id)
                .ToQueryString();
            Assert.Contains("OFFSET", sql);
            Assert.Contains("FETCH NEXT", sql);
        }
    }
}
