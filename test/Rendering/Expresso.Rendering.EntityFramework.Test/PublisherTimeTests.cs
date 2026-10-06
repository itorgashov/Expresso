using System;
using Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

namespace Expresso.Rendering.EntityFramework.Test
{
    public sealed class PublisherTimeTests
    {
        [Fact]
        public void OracleSelect_QuotesSchemaNamesAndExtractsClock()
        {
            var sql = PublisherTimes.OracleSelect(":p0,:p1");

            Assert.Contains("\"publisher\"", sql);
            Assert.Contains("\"id\"", sql);
            Assert.Contains("\"opens_at\"", sql);
            Assert.Contains("\"closes_at\"", sql);
            Assert.Contains("EXTRACT(HOUR FROM \"opens_at\")", sql);
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
