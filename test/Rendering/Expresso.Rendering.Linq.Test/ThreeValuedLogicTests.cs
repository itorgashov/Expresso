using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using static Expresso.Rendering.Linq.Test.TestRows;

namespace Expresso.Rendering.Linq.Test
{
    /// <summary>
    /// SQL three-valued logic: a comparison with a NULL operand is unknown, NOT(unknown) is unknown,
    /// TRUE OR unknown is TRUE, FALSE AND unknown is FALSE, and a WHERE clause keeps only TRUE rows.
    /// Every scenario runs on both profiles and must give the same ids.
    /// </summary>
    public class ThreeValuedLogicTests
    {
        // Setup: row 1 text "a" score 5, row 2 text NULL score 5, row 3 text "b" score NULL.
        private static List<Row> Rows() => new()
        {
            new Row { Id = 1, Text = "a", Score = 5, Flag = true },
            new Row { Id = 2, Text = null, Score = 5, Flag = null },
            new Row { Id = 3, Text = "b", Score = null, Flag = false },
        };

        private static void AssertBoth(BooleanFunction filter, params int[] expected)
        {
            Assert.Equal(expected, Ids(Rows(), filter));
            Assert.Equal(expected, QueryableIds(Rows(), filter));
        }

        [Fact]
        public void Neq_ExcludesNullOperand()
        {
            // Scenario: neq(text,"a") is unknown for row 2 (NULL), so only row 3 is kept.
            AssertBoth(new NeqFunc(Text, L("a")), 3);
        }

        [Fact]
        public void Not_OfUnknown_IsUnknown()
        {
            // Scenario: not(eq(text,"a")) for NULL text stays unknown, unlike C# where null != "a" is true.
            AssertBoth(new NotFunc(Eq(Text, "a")), 3);
        }

        [Fact]
        public void IsNull_IsNeverUnknown()
        {
            AssertBoth(new IsNullFunc(Text), 2);
            AssertBoth(new NotFunc(new IsNullFunc(Text)), 1, 3);
            AssertBoth(new IsNullFunc(Id));
        }

        [Fact]
        public void Or_TrueOrUnknown_IsTrue()
        {
            // Scenario: row 2 has eq(text,"x") unknown and eq(score,5) true, so the OR is true.
            AssertBoth(new OrFunc(new List<AbstractExpression> { Eq(Text, "x"), Eq(Score, 5) }), 1, 2);
        }

        [Fact]
        public void And_FalseAndUnknown_IsFalse_SoNotKeepsRow()
        {
            // Scenario: row 3 has eq(score,5) unknown and eq(text,"a") false; AND is FALSE so NOT(AND) is TRUE.
            // Row 2 has eq(score,5) true and eq(text,"a") unknown; AND is unknown so NOT(AND) drops it.
            var and = new AndFunc(new List<AbstractExpression> { Eq(Score, 5), Eq(Text, "a") });
            AssertBoth(and, 1);
            AssertBoth(new NotFunc(and), 3);
        }

        [Fact]
        public void Or_FalseOrUnknown_IsUnknown_SoNotDropsRow()
        {
            // Scenario: row 2: eq(text,"b") unknown OR eq(score,6) false is unknown; NOT keeps only rows where OR is FALSE.
            var or = new OrFunc(new List<AbstractExpression> { Eq(Text, "b"), Eq(Score, 6) });
            AssertBoth(or, 3);
            AssertBoth(new NotFunc(or), 1);
        }

        [Fact]
        public void In_WithNullOperand_IsUnknown()
        {
            AssertBoth(new InFunc(new List<AbstractExpression> { Text, L("a"), L("b") }), 1, 3);
            AssertBoth(new NotFunc(new InFunc(new List<AbstractExpression> { Text, L("a") })), 3);
        }

        [Fact]
        public void BooleanAsScalar_KeepsUnknown()
        {
            // Scenario: eq(gt(score,3), true): row 3 has gt unknown, so the eq is unknown (not false).
            var gtIsTrue = new EqFunc(new GtFunc(Score, L(3)), L(true));
            AssertBoth(gtIsTrue, 1, 2);
            AssertBoth(new NotFunc(gtIsTrue));
            AssertBoth(new IsNullFunc(new GtFunc(Score, L(3))), 3);
        }

        [Fact]
        public void BoolField_InAnd_UsesItsValue()
        {
            // Scenario: flag is a nullable bool column: TRUE for row 1, NULL (unknown) for row 2, FALSE for row 3.
            var and = new AndFunc(new List<AbstractExpression> { Flag, Eq(Id, 1) });
            AssertBoth(and, 1);
            AssertBoth(new NotFunc(new OrFunc(new List<AbstractExpression> { Flag, Eq(Id, 1) })), 3);
        }

        [Fact]
        public void ScalarMin_WithNullFirstArgument_ReturnsSecond()
        {
            // Scenario: SQL CASE WHEN a < b THEN a ELSE b: with a NULL the comparison is not TRUE, so the result is b.
            AssertBoth(Eq(new MinFunc(Score, L(10)), 10), 3);
            AssertBoth(Eq(new MinFunc(Score, L(10)), 5), 1, 2);
            // With b NULL and a not smaller, the result is NULL, so eq is unknown for row 3.
            AssertBoth(new IsNullFunc(new MaxFunc(L(10), Score)), 3);
            AssertBoth(Eq(new MaxFunc(L(10), Score), 10), 1, 2);
        }

        [Fact]
        public void All_IgnoresUnknownItems()
        {
            // Setup: row 1 children values 1 and NULL; row 2 child value -1; row 3 no children.
            // Scenario: all(children, gt(value,0)) is NOT EXISTS(item where NOT gt): NULL is unknown, not FALSE.
            var rows = new List<Row>
            {
                new() { Id = 1, Children = { new Child { Value = 1 }, new Child { Value = null } } },
                new() { Id = 2, Children = { new Child { Value = -1 } } },
                new() { Id = 3 },
            };
            var all = new AllFunc(Children, new GtFunc(Value, L(0)));
            Assert.Equal(new[] { 1, 3 }, Ids(rows, all));
            Assert.Equal(new[] { 1, 3 }, QueryableIds(rows, all));
            Assert.Equal(new[] { 1, 2, 3 }, Ids(rows, new AllFunc(Children)));
            Assert.Equal(new[] { 2 }, Ids(rows, new NotFunc(all)));
        }

        [Fact]
        public void AnyAndNone_UseOnlyTrueItems()
        {
            var rows = new List<Row>
            {
                new() { Id = 1, Children = { new Child { Value = null } } },
                new() { Id = 2, Children = { new Child { Value = 3 } } },
            };
            var any = new AnyFunc(Children, new GtFunc(Value, L(0)));
            Assert.Equal(new[] { 2 }, Ids(rows, any));
            Assert.Equal(new[] { 1 }, Ids(rows, new NoneFunc(Children, new GtFunc(Value, L(0)))));
            Assert.Equal(new[] { 1 }, Ids(rows, new NotFunc(any)));
        }
    }
}
