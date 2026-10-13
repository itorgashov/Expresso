using System;
using Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

namespace Expresso.Sample.NetFx.Test
{
    public sealed class PublisherTimeTests
    {
        [Theory]
        [InlineData(Expresso.Rendering.EntityFramework.Ef6Provider.Oracle)]
        [InlineData(Expresso.Rendering.EntityFramework.Ef6Provider.Sqlite)]
        [InlineData(Expresso.Rendering.EntityFramework.Ef6Provider.SqlServer)]
        public async System.Threading.Tasks.Task ProjectionMappings_AreCached_AndOracleTokensNeverLeakAsClocks(Expresso.Rendering.EntityFramework.Ef6Provider provider)
        {
            // Scenario: missing supplemental Oracle data must keep a selected clock null rather than expose publisher identity.
            var map = PublisherRepository.GetMapping(provider);
            Assert.Same(map, PublisherRepository.GetMapping(provider));
            var source = new[] { new Expresso.Sample.WebApi.NetFx.Ef6.Entities.Publisher { Id = 42, OpensAtText = "09:00:00", ClosesAtText = "17:30:00" } }.AsQueryable();
            var directive = new Expresso.Core.Projection.ProjectionDirective(new[] { new Expresso.Core.Projection.ProjectionSelection("opens") });
            var result = await new Expresso.Rendering.Linq.QueryableExpressionToLinqTransformer().RenderProjection(directive, map)
                .ExecuteAsync(source, new Executor());
            var clock = Assert.Single(Expresso.Rendering.Linq.ProjectionShaper.ToSparseObjects(result))["opensAt"];
            if (provider == Expresso.Rendering.EntityFramework.Ef6Provider.Oracle) Assert.Null(clock);
            if (provider == Expresso.Rendering.EntityFramework.Ef6Provider.Sqlite) Assert.Equal(TimeSpan.FromHours(9), clock);
        }

        [Fact]
        public void ReadSelectedClock_SqlNullIsRetainedAsNull()
        {
            var table = new System.Data.DataTable();
            table.Columns.Add("clock", typeof(string));
            table.Rows.Add(DBNull.Value);
            table.Rows.Add("09:00:00");
            using var reader = table.CreateDataReader();
            Assert.True(reader.Read());
            Assert.Null(PublisherTimes.ReadClock(reader, 0));
            Assert.True(reader.Read());
            Assert.Equal(TimeSpan.FromHours(9), PublisherTimes.ReadClock(reader, 0));
        }

        private sealed class Executor : Expresso.Rendering.Linq.IProjectionQueryExecutor
        {
            public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, System.Threading.CancellationToken cancellationToken) =>
                System.Threading.Tasks.Task.FromResult<System.Collections.Generic.IReadOnlyList<T>>(query.ToList());
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void OracleSelect_OnlyIncludesRequestedClocks(bool opens, bool closes)
        {
            var sql = PublisherTimes.OracleSelect(":p0", opens, closes);
            Assert.Equal(opens, sql.Contains("\"opens_at\""));
            Assert.Equal(closes, sql.Contains("\"closes_at\""));
            Assert.Contains("\"id\"", sql);
        }

        [Fact]
        public void OracleSelect_QuotesSchemaNamesAndExtractsClock()
        {
            var sql = PublisherTimes.OracleSelect(":p0,:p1");

            Assert.Contains("\"publisher\"", sql);
            Assert.Contains("\"id\"", sql);
            Assert.Contains("\"opens_at\"", sql);
            Assert.Contains("\"closes_at\"", sql);
            Assert.Contains("EXTRACT(HOUR FROM \"opens_at\")", sql);
            Assert.Contains("CASE WHEN \"opens_at\" IS NULL THEN NULL ELSE", sql);
            Assert.Contains("CASE WHEN \"closes_at\" IS NULL THEN NULL ELSE", sql);
            Assert.DoesNotContain("FROM publisher", sql);
        }

        [Theory]
        [InlineData("09:00:00", 9, 0, 0)]
        [InlineData("+000000000 09:00:00.000000000", 9, 0, 0)]
        [InlineData("+0 17:30:00", 17, 30, 0)]
        public void ParseTime_AcceptsClockAndIntervalText(string text, int hours, int minutes, int seconds)
        {
            Assert.Equal(new TimeSpan(hours, minutes, seconds), PublisherTimes.ParseTime(text));
        }
    }
}
