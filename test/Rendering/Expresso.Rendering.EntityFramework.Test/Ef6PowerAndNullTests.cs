using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;

namespace Expresso.Rendering.EntityFramework.Test
{
    /// <summary>SQL Server integer POWER, and isnull of abs/substring, keep the store call.</summary>
    public class Ef6PowerAndNullTests
    {
        [Fact]
        public void SqlServer_IntegerPower_KeepsTheBaseType()
        {
            // Scenario: POWER of an int base returns int. A float cast of the base changes a fractional exponent.
            using var context = new TestWidgetEf6Context();
            var fractional = context.WhereSql(Filter(new EqFunc(new PowerFunc(new Field("age", typeof(int)), new Literal(0.5)), new Literal(2.0))));
            var negative = context.WhereSql(Filter(new EqFunc(new PowerFunc(new Field("age", typeof(int)), new Literal(-1.0)), new Literal(0.0))));
            var code = context.WhereSql(Filter(new EqFunc(new PowerFunc(new Field("code", typeof(byte)), new Literal(0.5)), new Literal(2.0))));
            AssertIntegerPower(fractional);
            AssertIntegerPower(negative);
            AssertIntegerPower(code);
            var nested = context.WhereSql(Filter(new EqFunc(
                new PowerFunc(new PowerFunc(new Field("age", typeof(int)), new Literal(0.5)), new Literal(0.5)),
                new Literal(1.0))));
            var divided = context.WhereSql(Filter(new EqFunc(
                new DivFunc(new PowerFunc(new Field("age", typeof(int)), new Literal(1)), new Literal(2)),
                new Literal(2.0))));
            var absolute = context.WhereSql(Filter(new EqFunc(new AbsFunc(new PowerFunc(new Literal(-2), new Literal(31))), new Literal(0))));
            AssertIntegerComposition(nested);
            AssertIntegerComposition(divided);
            AssertIntegerComposition(absolute);
        }

        [Fact]
        public void SqlServer_IntegerFloorCeilingAndRound_StayInteger()
        {
            // Scenario: FLOOR, CEILING and ROUND of an int stay int, so the following division truncates.
            using var context = new TestWidgetEf6Context();
            var age = new Field("age", typeof(int));
            var code = new Field("code", typeof(byte));
            var amount = new Field("amount", typeof(double));
            foreach (var wrap in new Func<AbstractExpression, AbstractExpression>[]
            {
                value => new FloorFunc(value),
                value => new CeilingFunc(value),
                value => new RoundFunc(value, new Literal(0)),
            })
            {
                AssertIntegerDivision(context.WhereSql(Filter(Divided(wrap(new PowerFunc(age, new Literal(1)))))));
                AssertIntegerDivision(context.WhereSql(Filter(Divided(wrap(age)))));
                AssertIntegerDivision(context.WhereSql(Filter(Divided(wrap(code)))));
                AssertIntegerDivision(context.WhereSql(Filter(Divided(wrap(new Literal(5))))));
                var floating = context.WhereSql(Filter(new EqFunc(new DivFunc(wrap(amount), new Literal(2.0)), new Literal(2.5))));
                Assert.Contains("Amount", floating, StringComparison.OrdinalIgnoreCase);
            }

            var nullable = context.NullableSql(Filter(Divided(new FloorFunc(new Field("rank", typeof(int))))));
            AssertIntegerDivision(nullable);
            Assert.Contains("NULL", nullable, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SqlServer_IsNullIntegerRound_KeepsTheCall()
        {
            // Scenario: rounding the maximum int at -1 overflows. Dropping ROUND makes isnull false.
            using var context = new TestWidgetEf6Context();
            var age = new Field("age", typeof(int));
            var missing = context.WhereSql(Filter(new IsNullFunc(new RoundFunc(age, new Literal(-1)))));
            var present = context.WhereSql(Filter(new NotFunc(new IsNullFunc(new RoundFunc(age, new Literal(-1))))));
            var literal = context.WhereSql(Filter(new IsNullFunc(new RoundFunc(new Literal(int.MaxValue), new Literal(-1)))));
            var ordinary = context.WhereSql(Filter(new IsNullFunc(new RoundFunc(age, new Literal(0)))));
            var nullable = context.NullableSql(Filter(new IsNullFunc(new RoundFunc(new Field("rank", typeof(int)), new Literal(-1)))));
            Assert.Contains("ROUND", missing, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ROUND", present, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ROUND", literal, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ROUND", ordinary, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ROUND", nullable, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("NULL", nullable, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("1 = 0", missing);
        }

        [Fact]
        public void SqlServer_IsNullLeftAndRight_KeepTheCall()
        {
            // Scenario: SQL Server rejects a negative LEFT or RIGHT length. Dropping the call hides that error.
            using var context = new TestWidgetEf6Context();
            var name = new Field("name", typeof(string));
            var notes = new Field("notes", typeof(string));
            foreach (var function in new Func<AbstractExpression, AbstractExpression, AbstractExpression>[]
            {
                (source, length) => new LeftFunc(source, length),
                (source, length) => new RightFunc(source, length),
            })
            {
                var missing = context.WhereSql(Filter(new IsNullFunc(function(name, new Literal(-1)))));
                var present = context.WhereSql(Filter(new NotFunc(new IsNullFunc(function(name, new Literal(-1))))));
                var literal = context.WhereSql(Filter(new IsNullFunc(function(new Literal("ab"), new Literal(-1)))));
                var nullable = context.WhereSql(Filter(new IsNullFunc(function(notes, new Literal(-1)))));
                AssertKeepsStringCall(missing);
                AssertKeepsStringCall(present);
                AssertKeepsStringCall(literal);
                AssertKeepsStringCall(nullable);
                Assert.Contains("NULL", nullable, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void SqlServer_IsNullAdd_KeepsTheCall()
        {
            // Scenario: folding isnull of a non-nullable int sum hides overflow of the minimum and maximum int.
            using var context = new TestWidgetEf6Context();
            var sum = context.WhereSql(Filter(new IsNullFunc(new AddFunc(new Literal(int.MaxValue), new Literal(1)))));
            var difference = context.WhereSql(Filter(new NotFunc(new IsNullFunc(new SubFunc(new Field("age", typeof(int)), new Literal(1))))));
            Assert.Contains("+", sum);
            Assert.Contains("-", difference);
        }

        [Fact]
        public void IsNullAbsAndSubstring_KeepTheCall()
        {
            // Scenario: folding isnull to false hides an abs overflow and a negative substring length.
            using var sqlServer = new TestWidgetEf6Context();
            using var postgres = new PostgreSqlLeftRightContext();
            using var sqlite = new SqliteRightContext();
            foreach (var context in new[] { sqlServer.WhereSql, postgres.WhereSql, sqlite.WhereSql })
            {
                var absolute = context(Filter(new IsNullFunc(new AbsFunc(new Field("age", typeof(int))))));
                var minimum = context(Filter(new IsNullFunc(new AbsFunc(new Literal(int.MinValue)))));
                var slice = context(Filter(new IsNullFunc(new SubStringFunc(new Field("name", typeof(string)), new Literal(1), new Literal(-1)))));
                var negated = context(Filter(new NotFunc(new IsNullFunc(new AbsFunc(new Field("age", typeof(int)))))));
                var negatedSlice = context(Filter(new NotFunc(new IsNullFunc(new SubStringFunc(new Field("name", typeof(string)), new Literal(1), new Literal(-1))))));
                Assert.Contains("ABS", absolute, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("ABS", minimum, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("ABS", negated, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("SUBSTR", slice, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("SUBSTR", negatedSlice, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("1 = 0", absolute);
                Assert.DoesNotContain("1 = 0", minimum);
            }
        }

        private static EqFunc Divided(AbstractExpression value) =>
            new(new DivFunc(value, new Literal(2)), new Literal(2));

        private static void AssertIntegerDivision(string sql)
        {
            var compact = sql.Replace(" ", "");
            Assert.True(
                compact.Contains("/", StringComparison.Ordinal)
                && (compact.Contains("FLOOR(", StringComparison.OrdinalIgnoreCase)
                    || compact.Contains("CEILING(", StringComparison.OrdinalIgnoreCase)
                    || compact.Contains("ROUND(", StringComparison.OrdinalIgnoreCase))
                && !compact.Contains("ASfloat", StringComparison.OrdinalIgnoreCase)
                && !compact.Contains("POWER(CAST(", StringComparison.OrdinalIgnoreCase),
                sql);
        }

        private static void AssertIntegerPower(string sql)
        {
            var compact = sql.Replace(" ", "");
            Assert.Contains("CAST(POWER(", compact, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("POWER(CAST(", compact, StringComparison.OrdinalIgnoreCase);
        }

        private static void AssertIntegerComposition(string sql)
        {
            var compact = sql.Replace(" ", "");
            Assert.True(
                compact.Contains("POWER(", StringComparison.OrdinalIgnoreCase)
                && !compact.Contains("POWER(CAST(", StringComparison.OrdinalIgnoreCase)
                && !compact.Contains("ASfloat)/", StringComparison.OrdinalIgnoreCase)
                && !compact.Contains("ABS(CAST(", StringComparison.OrdinalIgnoreCase),
                sql);
        }

        private static void AssertKeepsStringCall(string sql) =>
            Assert.True(
                sql.Contains("LEFT", StringComparison.OrdinalIgnoreCase)
                || sql.Contains("RIGHT", StringComparison.OrdinalIgnoreCase)
                || sql.Contains("SUBSTRING", StringComparison.OrdinalIgnoreCase),
                sql);

        private static FilterCriteria Filter(BooleanFunction expression) => new() { Expression = expression };
    }
}
