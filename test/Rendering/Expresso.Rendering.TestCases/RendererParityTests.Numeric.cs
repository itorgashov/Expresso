using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.Rendering.TestCases
{
    public abstract partial class RendererParityTests
    {
        [Fact]
        public void GenerateWhereClause_NumericFunctions_ReturnDialectSql()
        {
            var age = D.Q("p.age");
            var salary = D.Q("p.salary");
            Assert.Equal($"({D.Mod(age, D.P(0))} = {D.P(1)})",
                RenderNumericEq(new ModFunc(new Field("age", typeof(int)), new Literal(2)), 0).whereClause);
            Assert.Equal($"({D.Floor(salary)} = {D.P(0)})",
                RenderNumericEq(new FloorFunc(new Field("salary", typeof(double))), 100.0).whereClause);
            Assert.Equal($"({D.Ceiling(salary)} = {D.P(0)})",
                RenderNumericEq(new CeilingFunc(new Field("salary", typeof(double))), 100.0).whereClause);
            Assert.Equal($"({D.Power(age, D.P(0))} = {D.P(1)})",
                RenderNumericEq(new PowerFunc(new Field("age", typeof(int)), new Literal(2)), 25.0).whereClause);
            Assert.Equal($"({D.Round(salary, "0")} = {D.P(0)})",
                RenderNumericEq(new RoundFunc(new Field("salary", typeof(double))), 19.99).whereClause);
        }

        [Fact]
        public void GenerateWhereClause_MinMax_ReturnsCaseSql()
        {
            var age = D.Q("p.age");
            var result = RenderNumericEq(new MinFunc(new Field("age", typeof(int)), new Literal(18)), 18);
            Assert.Equal($"((CASE WHEN {age} < {D.P(0)} THEN {age} ELSE {D.P(1)} END) = {D.P(2)})", result.whereClause);
        }

        [Fact]
        public void GenerateOrderBy_Floor_ReturnsFloorSql()
        {
            var sort = new SortDirective(new List<SortDirectiveItem>
            {
                new() { Expression = new FloorFunc(new Field("salary", typeof(double))), Direction = SortDirection.Ascending }
            });
            Assert.Equal($"{D.Floor(D.Q("p.salary"))} ASC", T.RenderOrderByClause(sort, RendererMaps.Numeric, D.Prefix).orderByClause);
        }

        private (string whereClause, Dictionary<string, object> parameters) RenderNumericEq(AbstractExpression left, object right)
        {
            FilterCriteria filter = new() { Expression = new EqFunc(left, new Literal(right)) };
            return T.RenderWhereClause(filter, RendererMaps.Numeric, D.Prefix);
        }
    }
}
