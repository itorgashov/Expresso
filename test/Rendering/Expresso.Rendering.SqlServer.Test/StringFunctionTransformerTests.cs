using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.SqlServer;

namespace Expresso.Tests.SqlServer
{
    public class StringFunctionTransformerTests
    {
        private readonly ExpressionToSqlServerQueryClauseTransformer _transformer = new();

        private readonly Dictionary<string, string> _fieldMap = new()
        {
            { "name", "name_col" },
            { "foo", "foo_col" },
        };

        private const string ParamPrefix = "param";

        [Fact]
        public void GenerateWhereClause_EndsWith_ReturnsLikeWithEscape()
        {
            FilterCriteria filter = new()
            {
                Expression = new StrEndswithFunc(new Field("name", typeof(string)), new Literal("hn"))
            };

            var result = _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);

            Assert.Equal("([name_col] LIKE @param_0 ESCAPE '\\')", result.whereClause);
            Assert.Equal("%hn", result.parameters["@param_0"]);
        }

        [Fact]
        public void GenerateWhereClause_Contains_ReturnsLikeWithEscape()
        {
            FilterCriteria filter = new()
            {
                Expression = new StrContainsFunc(new Field("name", typeof(string)), new Literal("ar"))
            };

            var result = _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);

            Assert.Equal("([name_col] LIKE @param_0 ESCAPE '\\')", result.whereClause);
            Assert.Equal("%ar%", result.parameters["@param_0"]);
        }

        [Fact]
        public void GenerateWhereClause_ContainsPercentLiteral_EscapesWildcards()
        {
            // Scenario: contains(title, "100%") must treat % as a literal, not a LIKE wildcard.
            FilterCriteria filter = new()
            {
                Expression = new StrContainsFunc(new Field("name", typeof(string)), new Literal("100%"))
            };

            var result = _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);

            Assert.Equal("([name_col] LIKE @param_0 ESCAPE '\\')", result.whereClause);
            Assert.Equal("%100\\%%", result.parameters["@param_0"]);
        }

        [Fact]
        public void GenerateWhereClause_StartsWithUnderscoreLiteral_EscapesWildcard()
        {
            FilterCriteria filter = new()
            {
                Expression = new StrStartswithFunc(new Field("name", typeof(string)), new Literal("a_b"))
            };

            var result = _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);

            Assert.Equal("a\\_b%", result.parameters["@param_0"]);
        }

        [Fact]
        public void GenerateWhereClause_Left_ReturnsLeftSql()
        {
            FilterCriteria filter = new()
            {
                Expression = new EqFunc(
                    new LeftFunc(new Field("name", typeof(string)), new Literal(1)),
                    new Literal("J"))
            };

            var result = _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);

            Assert.Equal("(LEFT([name_col], @param_0) = @param_1)", result.whereClause);
            Assert.Equal(1, result.parameters["@param_0"]);
            Assert.Equal("J", result.parameters["@param_1"]);
        }

        [Fact]
        public void GenerateWhereClause_Right_ReturnsRightSql()
        {
            FilterCriteria filter = new()
            {
                Expression = new EqFunc(
                    new RightFunc(new Field("name", typeof(string)), new Literal(1)),
                    new Literal("n"))
            };

            var result = _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);

            Assert.Equal("(RIGHT([name_col], @param_0) = @param_1)", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_Concat_ReturnsConcatSql()
        {
            FilterCriteria filter = new()
            {
                Expression = new EqFunc(
                    new ConcatFunc(new List<AbstractExpression>
                    {
                        new Field("name", typeof(string)),
                        new Literal(" "),
                        new Field("foo", typeof(string))
                    }),
                    new Literal("John Doe"))
            };

            var result = _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);

            Assert.Equal("(CONCAT([name_col], @param_0, [foo_col]) = @param_1)", result.whereClause);
            Assert.Equal(" ", result.parameters["@param_0"]);
            Assert.Equal("John Doe", result.parameters["@param_1"]);
        }

        [Fact]
        public void GenerateWhereClause_CaseAndTrimFunctions_ReturnSqlFunctions()
        {
            Assert.Equal(
                "(LOWER([name_col]) = @param_0)",
                _transformer.RenderWhereClause(EqStringFn(new LowerFunc(new Field("name", typeof(string))), "john"), _fieldMap, ParamPrefix).whereClause);
            Assert.Equal(
                "(UPPER([name_col]) = @param_0)",
                _transformer.RenderWhereClause(EqStringFn(new UpperFunc(new Field("name", typeof(string))), "JOHN"), _fieldMap, ParamPrefix).whereClause);
            Assert.Equal(
                "(TRIM([name_col]) = @param_0)",
                _transformer.RenderWhereClause(EqStringFn(new TrimFunc(new Field("name", typeof(string))), "John"), _fieldMap, ParamPrefix).whereClause);
            Assert.Equal(
                "(LTRIM([name_col]) = @param_0)",
                _transformer.RenderWhereClause(EqStringFn(new LTrimFunc(new Field("name", typeof(string))), "John"), _fieldMap, ParamPrefix).whereClause);
            Assert.Equal(
                "(RTRIM([name_col]) = @param_0)",
                _transformer.RenderWhereClause(EqStringFn(new RTrimFunc(new Field("name", typeof(string))), "John"), _fieldMap, ParamPrefix).whereClause);
        }

        [Fact]
        public void GenerateWhereClause_Len_ReturnsLenSql()
        {
            FilterCriteria filter = new()
            {
                Expression = new EqFunc(new LenFunc(new Field("name", typeof(string))), new Literal(4))
            };

            var result = _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);

            Assert.Equal("(LEN([name_col]) = @param_0)", result.whereClause);
            Assert.Equal(4, result.parameters["@param_0"]);
        }

        [Fact]
        public void GenerateWhereClause_Replace_ReturnsReplaceSql()
        {
            FilterCriteria filter = new()
            {
                Expression = new EqFunc(
                    new ReplaceFunc(new Field("name", typeof(string)), new Literal("J"), new Literal("K")),
                    new Literal("Kohn"))
            };

            var result = _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);

            Assert.Equal("(REPLACE([name_col], @param_0, @param_1) = @param_2)", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_IndexOf_TranslatesCharIndexToZeroBased()
        {
            FilterCriteria filter = new()
            {
                Expression = new GtFunc(
                    new IndexOfFunc(new Field("name", typeof(string)), new Literal("o")),
                    new Literal(0))
            };

            var result = _transformer.RenderWhereClause(filter, _fieldMap, ParamPrefix);

            Assert.Equal("((ISNULL(NULLIF(CHARINDEX(@param_0, [name_col]), 0), 0) - 1) > @param_1)", result.whereClause);
            Assert.Equal("o", result.parameters["@param_0"]);
            Assert.Equal(0, result.parameters["@param_1"]);
        }

        [Fact]
        public void GenerateOrderBy_Lower_ReturnsLowerSql()
        {
            var sort = new SortDirective(
            [
                new SortDirectiveItem
                {
                    Expression = new LowerFunc(new Field("name", typeof(string))),
                    Direction = SortDirection.Ascending
                }
            ]);

            var result = _transformer.RenderOrderByClause(sort, _fieldMap, ParamPrefix);

            Assert.Equal("LOWER([name_col]) ASC", result.orderByClause);
        }

        private static FilterCriteria EqStringFn(AbstractExpression left, string right) =>
            new() { Expression = new EqFunc(left, new Literal(right)) };
    }
}
