using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.SqlServer;

namespace Expresso.Tests.SqlServer
{
    public class NumericFunctionTransformerTests
    {
        private readonly ExpressionToSqlServerQueryClauseTransformer _transformer = new();

        private readonly Dictionary<string, string> _fieldMap = new()
        {
            { "age", "p.age" },
            { "salary", "p.salary" },
        };

        private const string ParamPrefix = "param";

        [Fact]
        public void GenerateWhereClause_Mod_ReturnsModSql()
        {
            var result = RenderEq(new ModFunc(AgeField(), new Literal(2)), 0);

            Assert.Equal("(([p].[age] % @param_0) = @param_1)", result.whereClause);
            Assert.Equal(2, result.parameters["@param_0"]);
            Assert.Equal(0, result.parameters["@param_1"]);
        }

        [Fact]
        public void GenerateWhereClause_Floor_ReturnsFloorSql()
        {
            var result = RenderEq(new FloorFunc(SalaryField()), 100.0);

            Assert.Equal("(FLOOR([p].[salary]) = @param_0)", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_Ceiling_ReturnsCeilingSql()
        {
            var result = RenderEq(new CeilingFunc(SalaryField()), 100.0);

            Assert.Equal("(CEILING([p].[salary]) = @param_0)", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_Sqrt_ReturnsSqrtSql()
        {
            var result = RenderEq(new SqrtFunc(AgeField()), 5.0);

            Assert.Equal("(SQRT([p].[age]) = @param_0)", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_Sign_ReturnsSignSql()
        {
            var result = RenderEq(new SignFunc(AgeField()), 1);

            Assert.Equal("(SIGN([p].[age]) = @param_0)", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_Power_ReturnsPowerSql()
        {
            var result = RenderEq(new PowerFunc(AgeField(), new Literal(2)), 25.0);

            Assert.Equal("(POWER([p].[age], @param_0) = @param_1)", result.whereClause);
            Assert.Equal(2, result.parameters["@param_0"]);
        }

        [Fact]
        public void GenerateWhereClause_RoundOneArg_ReturnsRoundWithZeroDigits()
        {
            var result = RenderEq(new RoundFunc(SalaryField()), 19.99);

            Assert.Equal("(ROUND([p].[salary], 0) = @param_0)", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_RoundTwoArgs_ReturnsRoundSql()
        {
            var result = RenderEq(new RoundFunc(SalaryField(), new Literal(-1)), 20.0);

            Assert.Equal("(ROUND([p].[salary], @param_0) = @param_1)", result.whereClause);
            Assert.Equal(-1, result.parameters["@param_0"]);
        }

        [Fact]
        public void GenerateWhereClause_Min_ReturnsCaseSql()
        {
            var result = RenderEq(new MinFunc(AgeField(), new Literal(18)), 18);

            Assert.Equal("((CASE WHEN [p].[age] < @param_0 THEN [p].[age] ELSE @param_1 END) = @param_2)", result.whereClause);
            Assert.Equal(18, result.parameters["@param_0"]);
            Assert.Equal(18, result.parameters["@param_1"]);
            Assert.Equal(18, result.parameters["@param_2"]);
        }

        [Fact]
        public void GenerateWhereClause_Max_ReturnsCaseSql()
        {
            var result = RenderEq(new MaxFunc(AgeField(), new Literal(65)), 65);

            Assert.Equal("((CASE WHEN [p].[age] > @param_0 THEN [p].[age] ELSE @param_1 END) = @param_2)", result.whereClause);
            Assert.Equal(65, result.parameters["@param_0"]);
            Assert.Equal(65, result.parameters["@param_1"]);
            Assert.Equal(65, result.parameters["@param_2"]);
        }

        [Fact]
        public void GenerateOrderByClause_Floor_ReturnsFloorSql()
        {
            var sort = new SortDirective(
            [
                new SortDirectiveItem
                {
                    Expression = new FloorFunc(SalaryField()),
                    Direction = SortDirection.Ascending,
                },
            ]);

            var result = _transformer.RenderOrderByClause(sort, _fieldMap, ParamPrefix);

            Assert.Equal("FLOOR([p].[salary]) ASC", result.orderByClause);
        }

        private static Field AgeField() => new("age", typeof(int));

        private static Field SalaryField() => new("salary", typeof(double));

        private (string whereClause, Dictionary<string, object> parameters) RenderEq(AbstractExpression left, double right)
        {
            FilterCriteria filter = new()
            {
                Expression = new EqFunc(left, new Literal(right))
            };
            return _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);
        }

        private (string whereClause, Dictionary<string, object> parameters) RenderEq(AbstractExpression left, int right)
        {
            FilterCriteria filter = new()
            {
                Expression = new EqFunc(left, new Literal(right))
            };
            return _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);
        }
    }
}
