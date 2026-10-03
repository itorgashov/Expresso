namespace Expresso.Rendering.Linq.Test
{
    /// <summary>PostgreSQL reference semantics of the in-memory function implementations.</summary>
    public class ExpressoFunctionsTests
    {
        [Theory]
        [InlineData(2.5, 0, 3.0)]
        [InlineData(-2.5, 0, -3.0)]
        [InlineData(2.675, 2, 2.68)]
        [InlineData(1234.5, -2, 1200.0)]
        [InlineData(1250.0, -2, 1300.0)]
        [InlineData(5.0, -30, 0.0)]
        [InlineData(1.23456, 40, 1.23456)]
        public void Round_IsHalfAwayFromZero(double value, int digits, double expected) =>
            Assert.Equal(expected, ExpressoFunctions.Round(value, digits));

        [Fact]
        public void Round_PassesThroughNonFiniteAndHugeValues()
        {
            Assert.True(double.IsNaN(ExpressoFunctions.Round(double.NaN, 0)));
            Assert.Equal(double.PositiveInfinity, ExpressoFunctions.Round(double.PositiveInfinity, 0));
            Assert.Equal(1e30, ExpressoFunctions.Round(1e30, 0));
        }

        [Theory]
        [InlineData("abc", 1, 2, "ab")]
        [InlineData("abc", 0, 2, "a")]
        [InlineData("abc", -5, 2, "")]
        [InlineData("abc", 2, 10, "bc")]
        [InlineData("abc", 5, 1, "")]
        [InlineData("abc", 1, 0, "")]
        public void Substring_DropsPositionsOutsideTheString(string source, int start, int length, string expected) =>
            Assert.Equal(expected, ExpressoFunctions.Substring(source, start, length));

        [Fact]
        public void Substring_NegativeLength_Throws() =>
            Assert.Throws<ArgumentException>(() => ExpressoFunctions.Substring("abc", 1, -1));

        [Theory]
        [InlineData("abcde", 2, "ab", "de")]
        [InlineData("abcde", 9, "abcde", "abcde")]
        [InlineData("abcde", -2, "abc", "cde")]
        [InlineData("abcde", -9, "", "")]
        [InlineData("abcde", 0, "", "")]
        public void LeftRight_FollowPostgreSql(string source, int length, string left, string right)
        {
            Assert.Equal(left, ExpressoFunctions.Left(source, length));
            Assert.Equal(right, ExpressoFunctions.Right(source, length));
        }

        [Fact]
        public void Trim_RemovesSpacesOnly()
        {
            Assert.Equal("\tx\t", ExpressoFunctions.Trim(" \tx\t "));
            Assert.Equal("x  ", ExpressoFunctions.LTrim("  x  "));
            Assert.Equal("  x", ExpressoFunctions.RTrim("  x  "));
        }

        [Fact]
        public void Replace_IsOrdinal_AndIgnoresEmptyOldValue()
        {
            Assert.Equal("aXa", ExpressoFunctions.Replace("aAa", "A", "X"));
            Assert.Equal("abc", ExpressoFunctions.Replace("abc", "", "X"));
        }

        [Fact]
        public void AddTimeOfDay_WrapsAt24Hours()
        {
            Assert.Equal(new TimeSpan(1, 0, 0), ExpressoFunctions.AddTimeOfDay(new TimeSpan(23, 0, 0), TimeSpan.FromHours(2)));
            Assert.Equal(new TimeSpan(23, 30, 0), ExpressoFunctions.AddTimeOfDay(new TimeSpan(0, 30, 0), TimeSpan.FromHours(-1)));
            Assert.Equal(new TimeSpan(10, 0, 0), ExpressoFunctions.AddTimeOfDay(new TimeSpan(10, 0, 0), TimeSpan.FromDays(3)));
        }

        [Fact]
        public void MinMax_IgnoreNulls_AndReturnNullForNone()
        {
            var values = new int?[] { null, 3, 1, null, 2 };
            Assert.Equal(1, ExpressoFunctions.Min(values, v => v));
            Assert.Equal(3, ExpressoFunctions.Max(values, v => v));
            Assert.Null(ExpressoFunctions.Min(new int?[] { null }, v => v));
            Assert.Null(ExpressoFunctions.Max(Array.Empty<string?>(), v => v));
            Assert.Equal("B", ExpressoFunctions.Min(new[] { "b", "B", "a" }, v => v));
        }

        [Fact]
        public void SortComparer_PutsNullLast_AndComparesStringsOrdinally()
        {
            var comparer = InMemorySortComparer.Instance;
            Assert.Equal(0, comparer.Compare(null, null));
            Assert.True(comparer.Compare(null, 1) > 0);
            Assert.True(comparer.Compare(1, null) < 0);
            Assert.True(comparer.Compare("B", "a") < 0);
            Assert.True(comparer.Compare(2, 10) < 0);
        }
    }
}
