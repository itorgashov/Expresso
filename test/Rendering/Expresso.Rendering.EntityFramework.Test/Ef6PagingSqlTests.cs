using System.Linq.Expressions;
using Expresso.Core.Paging;
using Expresso.Rendering.EntityFramework;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.EntityFramework.Test
{
    /// <summary>
    /// Scenario: EF6 translates <c>Page</c> on the SQL Server provider.
    /// Setup: <see cref="TestWidgetEf6Context"/> prints SQL with <c>ToString</c> and opens no connection.
    /// Expect: skip and take become parameterized <c>OFFSET</c>/<c>FETCH</c>, take-only becomes <c>TOP</c>, and <c>Skip</c> without <c>OrderBy</c> is rejected.
    /// </summary>
    public class Ef6PagingSqlTests
    {
        [Fact]
        public void SqlServer_SkipAndTake_UsesOffsetFetchParameters()
        {
            using var context = new TestWidgetEf6Context();
            var sql = context.Widgets.OrderBy(w => w.Id).Page(new PagingDirective(page: 2, pageSize: 2)).Select(w => w.Id).ToString();

            Assert.Contains("OFFSET", sql);
            Assert.Contains("FETCH NEXT", sql);
            Assert.Contains("@", sql);
            Assert.DoesNotContain("OFFSET 2", sql);
        }

        [Fact]
        public void SqlServer_TakeOnly_UsesTop()
        {
            using var context = new TestWidgetEf6Context();
            var sql = context.Widgets.Page(new PagingDirective(take: 2)).Select(w => w.Id).ToString();

            Assert.Contains("TOP", sql);
        }

        [Fact]
        public void SkipWithoutOrderBy_IsRejected()
        {
            using var context = new TestWidgetEf6Context();
            var query = context.Widgets.Page(new PagingDirective(skip: 1));
            Assert.Throws<NotSupportedException>(() => query.ToString());
        }

        [Fact]
        public void Sqlite_SkipOnly_UsesUnlimitedLimit()
        {
            // SQLite LIMIT -1 has no upper bound. Enumerable.Take rejects a negative count, so this checks the expression
            // and the SQL EF6 generates rather than executing the query in memory.
            var source = new[] { 1, 2, 3, 4, 5, 6 }.AsQueryable();
            var paged = source.Page(new PagingDirective(skip: 4), Ef6Provider.Sqlite);
            Assert.Contains(".Skip(", paged.Expression.ToString());
            Assert.Equal(-1, TakeCount(paged.Expression));

            using var context = new SqliteRightContext();
            var sql = context.Widgets.OrderBy(w => w.Id).Page(new PagingDirective(skip: 4), Ef6Provider.Sqlite).Select(w => w.Id).ToString();
            Assert.Contains("LIMIT", sql);
            Assert.Contains("OFFSET", sql);
            Assert.DoesNotContain("2147483647", sql);
        }

        [Fact]
        public void SqlServer_SkipOnly_LeavesTakeOff()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6 }.AsQueryable();
            var paged = source.Page(new PagingDirective(skip: 4), Ef6Provider.SqlServer);
            Assert.DoesNotContain(".Take(", paged.Expression.ToString());
            Assert.Equal(new[] { 5, 6 }, paged.ToArray());
        }

        private static int TakeCount(Expression expression)
        {
            var call = (MethodCallExpression)expression;
            Assert.Equal(nameof(Queryable.Take), call.Method.Name);
            var box = ((ConstantExpression)((MemberExpression)call.Arguments[1]).Expression!).Value!;
            return (int)box.GetType().GetField(nameof(ParameterBox<int>.Value))!.GetValue(box)!;
        }
    }
}