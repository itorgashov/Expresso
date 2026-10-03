using System.Linq.Expressions;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using static Expresso.Rendering.Linq.Test.TestRows;

namespace Expresso.Rendering.Linq.Test
{
    /// <summary>
    /// Every literal is read from a captured <see cref="ParameterBox{T}"/> (so providers bind it as a parameter),
    /// in the same visit order as the SQL renderer's <c>AddParameter</c>. Values the SQL renderer writes inline stay constants.
    /// </summary>
    public class ParameterizationTests
    {
        [Fact]
        public void Literals_AreBoxed_InVisitOrder()
        {
            var filter = new AndFunc(new List<AbstractExpression>
            {
                Eq(Text, "x"),
                new GtFunc(Score, L(3)),
                new InFunc(new List<AbstractExpression> { Text, L("a"), L("b") }),
                new StrContainsFunc(Text, L("%")),
            });

            var predicate = QueryableT.BuildPredicate(F(filter), Mapping());

            Assert.Equal(new object[] { "x", 3, "a", "b", "%" }, Boxes(predicate));
            Assert.Empty(Constants(predicate).Where(c => c.Value is not null && c.Value is not bool && !IsBox(c.Value)));
        }

        [Fact]
        public void RoundWithoutDigits_HasNoDigitsParameter()
        {
            Assert.Equal(new object[] { 3.0 }, Boxes(QueryableT.BuildPredicate(F(Eq(new RoundFunc(Amount), 3.0)), Mapping())));
            Assert.Equal(new object[] { 1, 3.0 }, Boxes(QueryableT.BuildPredicate(F(Eq(new RoundFunc(Amount, L(1)), 3.0)), Mapping())));
        }

        [Fact]
        public void AllWithoutPredicate_IsConstantTrue()
        {
            var predicate = QueryableT.BuildPredicate(F(new AllFunc(Children)), Mapping());
            Assert.Equal(true, Assert.IsType<ConstantExpression>(predicate.Body).Value);
        }

        [Fact]
        public void QueryableShape_GuardsNullableOperands_Only()
        {
            var text = QueryableT.BuildPredicate(F(Eq(Text, "x")), Mapping()).Body.ToString();
            Assert.Contains("(e.Text != null)", text);
            Assert.Contains("(e.Text == value(Expresso.Rendering.Linq.ParameterBox`1[System.String]).Value)", text);

            var id = QueryableT.BuildPredicate(F(Eq(Id, 1)), Mapping()).Body.ToString();
            Assert.Equal("(e.Id == value(Expresso.Rendering.Linq.ParameterBox`1[System.Int32]).Value)", id);

            var score = QueryableT.BuildPredicate(F(new NeqFunc(Score, L(1))), Mapping()).Body.ToString();
            Assert.Contains("(e.Score != null)", score);
            Assert.Contains("(e.Score.Value != value(", score);
        }

        [Fact]
        public void QueryableShape_UsesTranslatableMembers()
        {
            Assert.Contains(".Contains(", Body(new StrContainsFunc(Text, L("a"))));
            Assert.Contains(".Substring((value(", Body(Eq(new SubStringFunc(Text, L(1), L(2)), "a")));
            Assert.Contains(".Length", Body(Eq(new LenFunc(Text), 1)));
            Assert.Contains("Round(e.Amount.Value)", Body(Eq(new RoundFunc(Amount), 1.0)));
        }

        private static string Body(BooleanFunction filter) => QueryableT.BuildPredicate(F(filter), Mapping()).Body.ToString();

        private static object[] Boxes(Expression expression) =>
            Constants(expression).Where(c => c.Value is not null && IsBox(c.Value))
                .Select(c => c.Value!.GetType().GetField("Value")!.GetValue(c.Value)!)
                .ToArray();

        private static bool IsBox(object value) =>
            value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(ParameterBox<>);

        private static List<ConstantExpression> Constants(Expression expression)
        {
            var collector = new ConstantCollector();
            collector.Visit(expression);
            return collector.Found;
        }

        private sealed class ConstantCollector : ExpressionVisitor
        {
            public List<ConstantExpression> Found { get; } = new();

            protected override Expression VisitConstant(ConstantExpression node)
            {
                Found.Add(node);
                return base.VisitConstant(node);
            }
        }
    }
}
