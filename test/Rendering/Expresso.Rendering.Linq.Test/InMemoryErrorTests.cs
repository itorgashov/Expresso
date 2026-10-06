using Expresso.Core.CriteriaExpressions;
using static Expresso.Rendering.Linq.Test.TestRows;

namespace Expresso.Rendering.Linq.Test
{
    /// <summary>In-memory power, abs and substring follow the PostgreSQL error rules, including inside <c>isnull</c>.</summary>
    public class InMemoryErrorTests
    {
        private static List<Row> Rows() => new()
        {
            new Row { Id = 1, Amount = -1, Score = 2, Text = "ab", Code = 5 },
            new Row { Id = 2, Amount = 2, Score = null, Text = "cd" },
            new Row { Id = 3, Amount = 0.5, Score = 4, Text = null },
            new Row { Id = 4, Amount = 0, Score = 9, Text = "ef" },
        };

        [Fact]
        public void Power_OrdinaryValue_Matches()
        {
            // Scenario: a finite integer exponent is ordinary arithmetic. 2^2 is 4, and a null base stays null.
            Assert.Equal(new[] { 2 }, Ids(Rows(), Eq(new PowerFunc(Amount, L(2.0)), 4.0)));
            Assert.Equal(new[] { 2 }, Ids(Rows(), new IsNullFunc(new PowerFunc(Score, L(0.5)))));
        }

        [Fact]
        public void Power_SortsFiniteValues_AndLeavesNullLast()
        {
            var sort = Sort(new PowerFunc(Score, L(1.0)));
            Assert.Equal(new[] { 1, 3, 4, 2 }, Rows().OrderBy(InMemoryT, sort, Mapping()).Select(r => r.Id).ToArray());
        }

        [Fact]
        public void Power_MinimumSubnormal_StaysPositive()
        {
            // Scenario: 0.5^1074 is the smallest positive double. The next exponent underflows; this one does not.
            var rows = new List<Row> { new() { Id = 1, Amount = 0.5 } };
            Assert.Equal(new[] { 1 }, Ids(rows, new GtFunc(new PowerFunc(Amount, L(1074.0)), L(0.0))));
        }

        [Theory]
        [InlineData(-1.0, 0.5, "non-integer power")]
        [InlineData(0.0, -1.0, "zero raised to a negative power")]
        [InlineData(2.0, 1024.0, "overflow")]
        [InlineData(0.5, 1075.0, "underflow")]
        public void Power_DomainAndRange_Throw(double baseValue, double exponent, string reason)
        {
            var rows = new List<Row> { new() { Id = 1, Amount = baseValue } };
            var power = new PowerFunc(Amount, L(exponent));
            AssertMessage(reason, () => Ids(rows, new IsNullFunc(power)));
            AssertMessage(reason, () => Ids(rows, new NotFunc(new EqFunc(power, L(0.0)))));
            AssertMessage(reason, () => Ids(rows, new GtFunc(power, L(0.0))));
            AssertMessage(reason, () => rows.OrderBy(InMemoryT, Sort(power), Mapping()).ToList());
        }

        [Fact]
        public void Arithmetic_Range_Throws_AndModuloMinIsZero()
        {
            // Scenario: PostgreSQL rejects integer and float overflow. int.MinValue % -1 is 0, and isnull still evaluates it.
            var rows = new List<Row> { new() { Id = 1, Score = 1, Amount = 1 } };
            var missing = new List<Row> { new() { Id = 2, Score = null } };
            Assert.Equal(new[] { 1 }, Ids(rows, Eq(new AddFunc(Score, L(1)), 2)));
            Assert.Equal(new[] { 2 }, Ids(missing, new IsNullFunc(new AddFunc(Score, L(1)))));
            AssertMessage("integer out of range", () => Ids(rows, new GtFunc(new AddFunc(new Literal(int.MaxValue), L(1)), L(0))));
            AssertMessage("integer out of range", () => Ids(rows, new IsNullFunc(new AddFunc(new Literal(int.MaxValue), L(1)))));
            AssertMessage("integer out of range", () => Ids(rows, new NotFunc(new IsNullFunc(new SubFunc(new Literal(int.MinValue), L(1))))));
            AssertMessage("integer out of range", () => Ids(rows, Eq(new MultFunc(new Literal(65536), L(65536)), 0)));
            AssertMessage("integer out of range", () => Ids(rows, new IsNullFunc(new DivFunc(new Literal(int.MinValue), L(-1)))));
            Assert.Equal(new[] { 1 }, Ids(rows, Eq(new ModFunc(new Literal(int.MinValue), L(-1)), 0)));
            Assert.Equal(Array.Empty<int>(), Ids(rows, new IsNullFunc(new ModFunc(new Literal(int.MinValue), L(-1)))));
            AssertMessage("overflow", () => Ids(rows, new GtFunc(new AddFunc(new Literal(1e308), new Literal(1e308)), L(0.0))));
            AssertMessage("overflow", () => Ids(rows, new IsNullFunc(new MultFunc(new Literal(1e308), L(2.0)))));
            AssertMessage("overflow", () => Ids(rows, new GtFunc(new DivFunc(new Literal(1e308), new Literal(1e-308)), L(0.0))));
            AssertMessage("underflow", () => Ids(rows, Eq(new MultFunc(new Literal(1e-200), new Literal(1e-200)), 0.0)));
            AssertMessage("underflow", () => Ids(rows, new IsNullFunc(new DivFunc(new Literal(1e-200), new Literal(1e200)))));
            AssertMessage("integer out of range", () => rows.OrderBy(InMemoryT, Sort(new AddFunc(new Literal(int.MaxValue), L(1))), Mapping()).ToList());
        }

        [Fact]
        public void Round_OutsideDecimalBounds_StillRounds()
        {
            // Scenario: a magnitude past decimal.MaxValue and a precision past 28 still follow round(numeric, int).
            var huge = new List<Row> { new() { Id = 1, Amount = 1e29 } };
            var tiny = new List<Row> { new() { Id = 1, Amount = 1.234567e-29 } };
            Assert.Equal(new[] { 1 }, Ids(huge, Eq(new RoundFunc(Amount, L(-30)), 0.0)));
            Assert.Equal(new[] { 1 }, Ids(tiny, Eq(new RoundFunc(Amount, L(30)), 1.2e-29)));
            Assert.Equal(new[] { 1 }, huge.OrderBy(InMemoryT, Sort(new RoundFunc(Amount, L(-30))), Mapping()).Select(r => r.Id).ToArray());
        }

        [Fact]
        public void Round_ExtremePrecisionAndSubnormals_MatchTheValue()
        {
            // Scenario: maximum precision leaves 1e29 unchanged, and rounding the smallest double does not throw.
            var huge = new List<Row> { new() { Id = 1, Amount = 1e29 } };
            var tiny = new List<Row> { new() { Id = 1, Amount = double.Epsilon } };
            var negative = new List<Row> { new() { Id = 1, Amount = -double.Epsilon } };
            Assert.Equal(new[] { 1 }, Ids(huge, Eq(new RoundFunc(Amount, L(int.MaxValue)), 1e29)));
            Assert.Equal(new[] { 1 }, Ids(tiny, Eq(new RoundFunc(Amount, L(324)), double.Epsilon)));
            Assert.Equal(new[] { 1 }, Ids(negative, Eq(new RoundFunc(Amount, L(324)), -double.Epsilon)));
            Assert.Equal(new[] { 1 }, huge.OrderBy(InMemoryT, Sort(new RoundFunc(Amount, L(int.MaxValue))), Mapping()).Select(r => r.Id).ToArray());
            Assert.Equal(new[] { 1 }, tiny.OrderBy(InMemoryT, Sort(new RoundFunc(Amount, L(324))), Mapping()).Select(r => r.Id).ToArray());
            var midpoint = new List<Row> { new() { Id = 1, Amount = 1.005e-29 } };
            var scaled = new List<Row> { new() { Id = 1, Amount = 1.225e-28 } };
            var wide = new List<Row> { new() { Id = 1, Amount = 1.234567890123456 } };
            Assert.Equal(new[] { 1 }, Ids(midpoint, Eq(new RoundFunc(Amount, L(31)), 1.01e-29)));
            Assert.Equal(new[] { 1 }, Ids(scaled, Eq(new RoundFunc(Amount, L(30)), 1.23e-28)));
            Assert.Equal(new[] { 1 }, Ids(wide, Eq(new RoundFunc(Amount, L(30)), 1.23456789012346)));
            Assert.Equal(new[] { 1 }, midpoint.OrderBy(InMemoryT, Sort(new RoundFunc(Amount, L(31))), Mapping()).Select(r => r.Id).ToArray());
            var half = new List<Row> { new() { Id = 1, Amount = 5e-29 } };
            var doubled = new List<Row> { new() { Id = 1, Amount = 1.499e-27 } };
            Assert.Equal(new[] { 1 }, Ids(half, Eq(new RoundFunc(Amount, L(28)), 1e-28)));
            Assert.Equal(new[] { 1 }, Ids(doubled, Eq(new RoundFunc(Amount, L(27)), 1e-27)));
            Assert.Equal(new[] { 1 }, Ids(new List<Row> { new() { Id = 1, Amount = -5e-29 } }, Eq(new RoundFunc(Amount, L(28)), -1e-28)));
            Assert.Equal(new[] { 1 }, Ids(new List<Row> { new() { Id = 1, Amount = -1.499e-27 } }, Eq(new RoundFunc(Amount, L(27)), -1e-27)));
            Assert.Equal(new[] { 1 }, Ids(new List<Row> { new() { Id = 1, Amount = 4.99999999999999e-29 } }, Eq(new RoundFunc(Amount, L(28)), 0.0)));
            Assert.Equal(new[] { 1 }, Ids(doubled, Eq(new RoundFunc(Amount, L(28)), 1.5e-27)));
            Assert.Equal(new[] { 1 }, half.OrderBy(InMemoryT, Sort(new RoundFunc(Amount, L(28))), Mapping()).Select(r => r.Id).ToArray());
            Assert.Equal(new[] { 1 }, doubled.OrderBy(InMemoryT, Sort(new RoundFunc(Amount, L(27))), Mapping()).Select(r => r.Id).ToArray());
        }

        [Fact]
        public void Round_Precision28And29_CompareEqual()
        {
            // Scenario: both precisions keep one 15-digit decimal, so the filter matches and the sort key is that double.
            var rows = new List<Row> { new() { Id = 1, Amount = 2.3490724761267527e-14 } };
            var negative = new List<Row> { new() { Id = 1, Amount = -2.3490724761267527e-14 } };
            var other = new List<Row> { new() { Id = 1, Amount = 5.346184712902729e-14 } };
            var ordinary = new List<Row> { new() { Id = 1, Amount = 2.5 } };
            Assert.Equal(new[] { 1 }, Ids(rows, new EqFunc(new RoundFunc(Amount, L(28)), new RoundFunc(Amount, L(29)))));
            Assert.Equal(new[] { 1 }, Ids(rows, Eq(new RoundFunc(Amount, L(28)), 2.34907247612675e-14)));
            Assert.Equal(new[] { 1 }, Ids(negative, new EqFunc(new RoundFunc(Amount, L(28)), new RoundFunc(Amount, L(29)))));
            Assert.Equal(new[] { 1 }, Ids(negative, Eq(new RoundFunc(Amount, L(28)), -2.34907247612675e-14)));
            Assert.Equal(new[] { 1 }, Ids(other, new EqFunc(new RoundFunc(Amount, L(28)), new RoundFunc(Amount, L(29)))));
            Assert.Equal(new[] { 1 }, Ids(ordinary, Eq(new RoundFunc(Amount, L(0)), 3.0)));
            Assert.Equal(new[] { 1 }, Ids(ordinary, new EqFunc(new RoundFunc(Amount, L(28)), new RoundFunc(Amount, L(29)))));
            Assert.Equal(new[] { 1 }, rows.OrderBy(InMemoryT, Sort(new RoundFunc(Amount, L(28))), Mapping()).Select(r => r.Id).ToArray());
        }

        [Fact]
        public void Round_ExactIntegerMidpoint_TiesToEven()
        {
            // Scenario: 1000000000000005 is an exact midpoint at 15 digits. Both precisions keep 1000000000000000.
            var rows = new List<Row> { new() { Id = 1, Amount = 1000000000000005d } };
            var negative = new List<Row> { new() { Id = 1, Amount = -1000000000000005d } };
            var below = new List<Row> { new() { Id = 1, Amount = 1000000000000004d } };
            var above = new List<Row> { new() { Id = 1, Amount = 1000000000000006d } };
            var odd = new List<Row> { new() { Id = 1, Amount = 1125899906842635d } };
            Assert.Equal(new[] { 1 }, Ids(rows, Eq(new RoundFunc(Amount, L(0)), 1000000000000000d)));
            Assert.Equal(new[] { 1 }, Ids(rows, Eq(new RoundFunc(Amount, L(28)), 1000000000000000d)));
            Assert.Equal(new[] { 1 }, Ids(negative, Eq(new RoundFunc(Amount, L(0)), -1000000000000000d)));
            Assert.Equal(new[] { 1 }, Ids(below, Eq(new RoundFunc(Amount, L(0)), 1000000000000000d)));
            Assert.Equal(new[] { 1 }, Ids(above, Eq(new RoundFunc(Amount, L(0)), 1000000000000010d)));
            Assert.Equal(new[] { 1 }, Ids(odd, Eq(new RoundFunc(Amount, L(0)), 1125899906842640d)));
            Assert.Equal(new[] { 1 }, Ids(new List<Row> { new() { Id = 1, Amount = 2.5 } }, Eq(new RoundFunc(Amount, L(0)), 3.0)));
            Assert.Equal(new[] { 1 }, rows.OrderBy(InMemoryT, Sort(new RoundFunc(Amount, L(0))), Mapping()).Select(r => r.Id).ToArray());
        }

        [Fact]
        public void Round_SmallPowerOfTen_MatchesTheField_AndMaximumOverflows()
        {
            // Scenario: rounding 1e-106 at 300 places keeps that double. The largest finite double does not fit.
            var rows = new List<Row> { new() { Id = 1, Amount = 1e-106 } };
            var negative = new List<Row> { new() { Id = 1, Amount = -1e-106 } };
            var round = new RoundFunc(Amount, L(300));
            Assert.Equal(new[] { 1 }, Ids(rows, new EqFunc(round, Amount)));
            Assert.Equal(new[] { 1 }, Ids(negative, new EqFunc(new RoundFunc(Amount, L(300)), Amount)));
            Assert.Equal(new[] { 1 }, rows.OrderBy(InMemoryT, Sort(round), Mapping()).Select(r => r.Id).ToArray());
            var maximum = new List<Row> { new() { Id = 1, Amount = double.MaxValue } };
            var minimum = new List<Row> { new() { Id = 1, Amount = double.MinValue } };
            AssertMessage("overflow", () => Ids(maximum, new EqFunc(new RoundFunc(Amount, L(300)), Amount)));
            AssertMessage("overflow", () => minimum.OrderBy(InMemoryT, Sort(new RoundFunc(Amount, L(0))), Mapping()).ToList());
        }

        [Fact]
        public void AbsMinInt_AndNegativeSubstring_ThrowInsideIsNull()
        {
            // Scenario: isnull must evaluate the call. PostgreSQL rejects both; a false answer would hide the error.
            var rows = new List<Row> { new() { Id = 1, Score = 1, Text = "ab" } };
            AssertMessage("integer out of range", () => Ids(rows, new IsNullFunc(new AbsFunc(new Literal(int.MinValue)))));
            AssertMessage("integer out of range", () => Ids(rows, new NotFunc(new IsNullFunc(new AbsFunc(new Literal(int.MinValue))))));
            AssertMessage("negative substring length", () => Ids(rows, new IsNullFunc(new SubStringFunc(Text, L(1), L(-1)))));
            Assert.Equal(new[] { 1 }, Ids(rows, Eq(new SubStringFunc(Text, L(1), L(2)), "ab")));
        }

        private static void AssertMessage(string reason, Action act)
        {
            var ex = Assert.Throws<NotSupportedException>(act);
            Assert.Contains(reason, ex.Message);
        }
    }
}
