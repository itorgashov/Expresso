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
        [InlineData(1e29, -30, 0.0)]
        [InlineData(1.234567e-29, 30, 1.2e-29)]
        public void Round_IsHalfAwayFromZero(double value, int digits, double expected) =>
            Assert.Equal(expected, ExpressoFunctions.Round(value, digits));

        [Fact]
        public void Round_PassesThroughNonFiniteAndHugeValues()
        {
            Assert.True(double.IsNaN(ExpressoFunctions.Round(double.NaN, 0)));
            Assert.Equal(double.PositiveInfinity, ExpressoFunctions.Round(double.PositiveInfinity, 0));
            Assert.Equal(1e30, ExpressoFunctions.Round(1e30, 0));
        }

        [Fact]
        public void Round_ExtremePrecisionAndSubnormals_DoNotWrapOrThrow()
        {
            // Scenario: adding int.MaxValue to a positive exponent must not wrap, and a subnormal must not divide by zero.
            Assert.Equal(1e29, ExpressoFunctions.Round(1e29, int.MaxValue));
            Assert.Equal(0d, ExpressoFunctions.Round(1e29, int.MinValue));
            Assert.Equal(double.Epsilon, ExpressoFunctions.Round(double.Epsilon, 324));
            Assert.Equal(-double.Epsilon, ExpressoFunctions.Round(-double.Epsilon, 324));
            Assert.Equal(0d, ExpressoFunctions.Round(double.Epsilon, 0));
            var nearby = double.Epsilon * 4;
            Assert.Equal(nearby, ExpressoFunctions.Round(nearby, 324));
            Assert.Equal(-nearby, ExpressoFunctions.Round(-nearby, 324));
            Assert.Equal(1.01e-29, ExpressoFunctions.Round(1.005e-29, 31));
            Assert.Equal(-1.01e-29, ExpressoFunctions.Round(-1.005e-29, 31));
            Assert.Equal(1.23e-28, ExpressoFunctions.Round(1.225e-28, 30));
            Assert.Equal(1.23456789012346, ExpressoFunctions.Round(1.234567890123456, 30));
            Assert.Equal(1e-28, ExpressoFunctions.Round(5e-29, 28));
            Assert.Equal(-1e-28, ExpressoFunctions.Round(-5e-29, 28));
            Assert.Equal(1e-27, ExpressoFunctions.Round(1.499e-27, 27));
            Assert.Equal(-1e-27, ExpressoFunctions.Round(-1.499e-27, 27));
            Assert.Equal(0d, ExpressoFunctions.Round(4.99999999999999e-29, 28));
            Assert.Equal(1.5e-27, ExpressoFunctions.Round(1.499e-27, 28));
        }

        [Theory]
        [InlineData(2.3490724761267527e-14, 2.34907247612675e-14)]
        [InlineData(-2.3490724761267527e-14, -2.34907247612675e-14)]
        [InlineData(5.346184712902729e-14, 5.34618471290273e-14)]
        [InlineData(-5.346184712902729e-14, -5.34618471290273e-14)]
        [InlineData(2.5, 2.5)]
        [InlineData(1.23456, 1.23456)]
        public void Round_Precision28And29_AreTheSameDouble(double value, double fifteenDigits)
        {
            var at28 = ExpressoFunctions.Round(value, 28);
            Assert.Equal(fifteenDigits, at28);
            Assert.Equal(at28, ExpressoFunctions.Round(value, 29));
        }

        [Theory]
        [InlineData(1000000000000005d, 1000000000000000d)]
        [InlineData(-1000000000000005d, -1000000000000000d)]
        [InlineData(1125899906842625d, 1125899906842620d)]
        [InlineData(-1125899906842625d, -1125899906842620d)]
        [InlineData(1000000000000004d, 1000000000000000d)]
        [InlineData(1000000000000006d, 1000000000000010d)]
        [InlineData(1125899906842635d, 1125899906842640d)]
        public void Round_FifteenDigitMidpoints_TieToEven(double value, double expected)
        {
            // Scenario: the 15-digit stage breaks an exact tie to even. Expectations are the integer quotient, not a formatter.
            Assert.Equal(expected, ExpressoFunctions.Round(value, 0));
            Assert.Equal(expected, ExpressoFunctions.Round(value, 28));
        }

        [Theory]
        [InlineData(1e-106)]
        [InlineData(-1e-106)]
        public void Round_SmallPowerOfTen_RoundTrips(double value)
        {
            Assert.Equal(value, ExpressoFunctions.Round(value, 300));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Round_MaximumFinite_IsOverflow(bool negative)
        {
            var value = negative ? double.MinValue : double.MaxValue;
            var atZero = Assert.Throws<NotSupportedException>(() => ExpressoFunctions.Round(value, 0));
            var atWide = Assert.Throws<NotSupportedException>(() => ExpressoFunctions.Round(value, 300));
            Assert.Contains("overflow", atZero.Message);
            Assert.Contains("overflow", atWide.Message);
        }

        [Fact]
        public void Round_VariedSignificands_KeepOneDoubleAcrossAdjacentPrecisions()
        {
            // Scenario: each 15-digit magnitude from 1e-14 upward already ends on or before the 28th place.
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            var heads = new[] { "1.25", "2.34907247612675", "5.34618471290273", "9.5" };
            for (var exponent = -14; exponent <= 27; exponent++)
            {
                foreach (var head in heads)
                {
                    var magnitude = double.Parse(head + "E" + exponent.ToString(culture), culture);
                    foreach (var signed in new[] { magnitude, -magnitude })
                    {
                        var at28 = ExpressoFunctions.Round(signed, 28);
                        Assert.Equal(signed, at28);
                        Assert.Equal(at28, ExpressoFunctions.Round(signed, 29));
                    }
                }
            }
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
            Assert.Throws<NotSupportedException>(() => ExpressoFunctions.Substring("abc", 1, -1));

        [Theory]
        [InlineData("😀a", 1, 100, "😀a")]
        [InlineData("😀a", 3, 1, "")]
        [InlineData("a😀b", 2, 2, "😀b")]
        public void Substring_CodePointBounds_DoNotThrow(string source, int start, int length, string expected) =>
            Assert.Equal(expected, ExpressoFunctions.Substring(source, start, length));

        [Fact]
        public void Sqrt_Negative_IsDomainError() =>
            Assert.Throws<NotSupportedException>(() => ExpressoFunctions.Sqrt(-1));

        [Fact]
        public void LengthAndIndexOf_UseCodePointsForSurrogates()
        {
            const string text = "a😀b";
            Assert.Equal(3, ExpressoFunctions.Length(text));
            Assert.Equal(1, ExpressoFunctions.IndexOf(text, "😀"));
            Assert.Equal("a😀", ExpressoFunctions.Left(text, 2));
            Assert.Equal("😀b", ExpressoFunctions.Right(text, 2));
        }

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
