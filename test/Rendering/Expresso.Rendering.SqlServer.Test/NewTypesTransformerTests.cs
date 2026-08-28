using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.SqlServer;

namespace Expresso.Tests.SqlServer
{
    public class NewTypesTransformerTests
    {
        private readonly ExpressionToSqlServerQueryClauseTransformer _transformer = new();
        private readonly Dictionary<string, string> _fieldMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "id", "b.id" },
            { "createdat", "b.created_at" },
        };

        private const string ParamPrefix = "param";

        [Fact]
        public void GenerateWhereClause_GuidEq_ReturnsParameterizedSql()
        {
            var guid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
            FilterCriteria filter = new()
            {
                Expression = new EqFunc(new Field("id", typeof(Guid)), new Literal(guid))
            };

            var result = _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);

            Assert.Equal("([b].[id] = @param_0)", result.whereClause);
            Assert.Equal(guid, result.parameters["@param_0"]);
        }

#if NET6_0_OR_GREATER
        [Fact]
        public void GenerateWhereClause_Time_ReturnsCastSql()
        {
            var result = RenderEq(
                new TimeFunc(new Field("createdat", typeof(DateTime))),
                new Literal(new TimeOnly(14, 30, 0)));

            Assert.Equal("(CAST([b].[created_at] AS time) = @param_0)", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_DateStringLiteral_ReturnsCastOnParameter()
        {
            var result = RenderEq(
                new DateFunc(new Literal("2020-01-01")),
                new Literal(new DateOnly(2020, 1, 1)));

            Assert.Equal("(CAST(@param_0 AS date) = @param_1)", result.whereClause);
            Assert.Equal("2020-01-01", result.parameters["@param_0"]);
        }
#endif

        private (string whereClause, Dictionary<string, object> parameters) RenderEq(
            AbstractExpression left,
            AbstractExpression right)
        {
            FilterCriteria filter = new()
            {
                Expression = new EqFunc(left, right)
            };
            return _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);
        }
    }
}
