using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.Rendering.TestCases
{
    public abstract partial class RendererParityTests
    {
        protected abstract DialectSql D { get; }

        protected IExpressionToQueryClauseTransformer T => D.Transformer;

        [Fact]
        public void GenerateWhereClause_AndFunc_ReturnsCorrectSql()
        {
            FilterCriteria filter = new()
            {
                Expression = new AndFunc(new List<AbstractExpression>
                {
                    new EqFunc(new Field("name", typeof(string)), new Literal("John")),
                    new GtFunc(new Field("age", typeof(int)), new Literal(25))
                })
            };

            var result = T.RenderWhereClause(filter, RendererMaps.Standard, D.Prefix);

            Assert.Equal($"(({D.Q("name_col")} = {D.P(0)}) AND ({D.Q("age_col")} > {D.P(1)}))", result.whereClause);
            Assert.Equal("John", result.parameters[D.P(0)]);
            Assert.Equal(25, result.parameters[D.P(1)]);
        }

        [Fact]
        public void GenerateWhereClause_OrFunc_ReturnsCorrectSql()
        {
            FilterCriteria filter = new()
            {
                Expression = new OrFunc(new List<AbstractExpression>
                {
                    new EqFunc(new Field("name", typeof(string)), new Literal("John")),
                    new GtFunc(new Field("age", typeof(int)), new Literal(25))
                })
            };

            var result = T.RenderWhereClause(filter, RendererMaps.Standard, D.Prefix);
            Assert.Equal($"(({D.Q("name_col")} = {D.P(0)}) OR ({D.Q("age_col")} > {D.P(1)}))", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_NotFunc_ReturnsCorrectSql()
        {
            FilterCriteria filter = new()
            {
                Expression = new NotFunc(new EqFunc(new Field("name", typeof(string)), new Literal("John")))
            };

            var result = T.RenderWhereClause(filter, RendererMaps.Standard, D.Prefix);
            Assert.Equal($"NOT (({D.Q("name_col")} = {D.P(0)}))", result.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_Comparisons_ReturnCorrectSql()
        {
            Assert.Equal($"({D.Q("name_col")} = {D.P(0)})",
                T.RenderWhereClause(EqName("John"), RendererMaps.Standard, D.Prefix).whereClause);
            Assert.Equal($"({D.Q("name_col")} != {D.P(0)})",
                T.RenderWhereClause(new() { Expression = new NeqFunc(new Field("name", typeof(string)), new Literal("John")) }, RendererMaps.Standard, D.Prefix).whereClause);
            Assert.Equal($"({D.Q("age_col")} > {D.P(0)})",
                T.RenderWhereClause(new() { Expression = new GtFunc(new Field("age", typeof(int)), new Literal(25)) }, RendererMaps.Standard, D.Prefix).whereClause);
            Assert.Equal($"({D.Q("age_col")} >= {D.P(0)})",
                T.RenderWhereClause(new() { Expression = new GteFunc(new Field("age", typeof(int)), new Literal(25)) }, RendererMaps.Standard, D.Prefix).whereClause);
            Assert.Equal($"({D.Q("age_col")} < {D.P(0)})",
                T.RenderWhereClause(new() { Expression = new LtFunc(new Field("age", typeof(int)), new Literal(30)) }, RendererMaps.Standard, D.Prefix).whereClause);
            Assert.Equal($"({D.Q("age_col")} <= {D.P(0)})",
                T.RenderWhereClause(new() { Expression = new LteFunc(new Field("age", typeof(int)), new Literal(30)) }, RendererMaps.Standard, D.Prefix).whereClause);
        }

        [Fact]
        public void GenerateWhereClause_Arithmetic_ReturnsCorrectSql()
        {
            var age = new Field("age", typeof(int));
            Assert.Equal($"(({D.Q("age_col")} + {D.P(0)}) = {D.P(1)})",
                T.RenderWhereClause(new() { Expression = new EqFunc(new AddFunc(age, new Literal(1)), new Literal(30)) }, RendererMaps.Standard, D.Prefix).whereClause);
            Assert.Equal($"(({D.Q("age_col")} - {D.P(0)}) = {D.P(1)})",
                T.RenderWhereClause(new() { Expression = new EqFunc(new SubFunc(age, new Literal(1)), new Literal(30)) }, RendererMaps.Standard, D.Prefix).whereClause);
            Assert.Equal($"(({D.Q("age_col")} * {D.P(0)}) = {D.P(1)})",
                T.RenderWhereClause(new() { Expression = new EqFunc(new MultFunc(age, new Literal(1)), new Literal(30)) }, RendererMaps.Standard, D.Prefix).whereClause);
            Assert.Equal($"(({D.Q("age_col")} / {D.P(0)}) = {D.P(1)})",
                T.RenderWhereClause(new() { Expression = new EqFunc(new DivFunc(age, new Literal(1)), new Literal(30)) }, RendererMaps.Standard, D.Prefix).whereClause);
            Assert.Equal($"(ABS({D.Q("age_col")}) = {D.P(0)})",
                T.RenderWhereClause(new() { Expression = new EqFunc(new AbsFunc(age), new Literal(30)) }, RendererMaps.Standard, D.Prefix).whereClause);
        }

        [Fact]
        public void GenerateWhereClause_InAndIsNull_ReturnsCorrectSql()
        {
            var inFilter = new FilterCriteria
            {
                Expression = new InFunc(new List<AbstractExpression>
                {
                    new Field("name", typeof(string)),
                    new Literal("John"),
                    new Literal("Jane")
                })
            };
            var result = T.RenderWhereClause(inFilter, RendererMaps.Standard, D.Prefix);
            Assert.Equal($"({D.Q("name_col")} IN ({D.P(0)}, {D.P(1)}))", result.whereClause);

            var isNull = T.RenderWhereClause(
                new() { Expression = new IsNullFunc(new Field("name", typeof(string))) },
                RendererMaps.Standard,
                D.Prefix);
            Assert.Equal($"({D.Q("name_col")} IS NULL)", isNull.whereClause);
            Assert.Empty(isNull.parameters);
        }

        [Fact]
        public void GenerateWhereClause_NullAndUnsupported_Throw()
        {
            Assert.Equal("filterCriteria", Assert.Throws<ArgumentNullException>(() => T.RenderWhereClause(null!, RendererMaps.Standard, D.Prefix)).ParamName);
            Assert.Equal("filterCriteria", Assert.Throws<ArgumentException>(() => T.RenderWhereClause(new() { Expression = null! }, RendererMaps.Standard, D.Prefix)).ParamName);
            Assert.Equal("fieldToColumnMap", Assert.Throws<ArgumentNullException>(() => T.RenderWhereClause(new() { Expression = new DummyBooleanFunction() }, (Dictionary<string, string>)null!, D.Prefix)).ParamName);
            Assert.Equal("paramNamePrefix", Assert.Throws<ArgumentNullException>(() => T.RenderWhereClause(new() { Expression = new DummyBooleanFunction() }, RendererMaps.Standard, null!)).ParamName);
            Assert.Equal("paramNamePrefix", Assert.Throws<ArgumentException>(() => T.RenderWhereClause(new() { Expression = new DummyBooleanFunction() }, RendererMaps.Standard, "2a")).ParamName);
            Assert.Throws<NotSupportedException>(() => T.RenderWhereClause(new() { Expression = new DummyBooleanFunction() }, RendererMaps.Standard, D.Prefix));
        }

        [Fact]
        public void GenerateOrderBy_FieldsAndBoolean_ReturnsCorrectSql()
        {
            var nameAsc = new SortDirective(new List<SortDirectiveItem>
            {
                new() { Expression = new Field("name", typeof(string)), Direction = SortDirection.Ascending }
            });
            Assert.Equal($"{D.Q("name_col")} ASC", T.RenderOrderByClause(nameAsc, RendererMaps.Standard, D.Prefix).orderByClause);

            var multi = new SortDirective(new List<SortDirectiveItem>
            {
                new() { Expression = new Field("name", typeof(string)), Direction = SortDirection.Ascending },
                new() { Expression = new Field("age", typeof(int)), Direction = SortDirection.Descending }
            });
            Assert.Equal($"{D.Q("name_col")} ASC, {D.Q("age_col")} DESC", T.RenderOrderByClause(multi, RendererMaps.Standard, D.Prefix).orderByClause);

            var boolSort = new SortDirective(new List<SortDirectiveItem>
            {
                new() { Expression = new EqFunc(new Field("age", typeof(int)), new Literal(1)), Direction = SortDirection.Ascending }
            });
            var boolResult = T.RenderOrderByClause(boolSort, RendererMaps.Standard, D.Prefix);
            Assert.Equal($"(CASE WHEN ({D.Q("age_col")} = {D.P(0)}) THEN 1 ELSE 0 END) ASC", boolResult.orderByClause);
        }

        [Fact]
        public void GenerateOrderBy_Invalid_Throws()
        {
            Assert.Equal("sortDirective", Assert.Throws<ArgumentNullException>(() => T.RenderOrderByClause(null!, RendererMaps.Standard, D.Prefix)).ParamName);
            Assert.Equal("sortDirective", Assert.Throws<ArgumentException>(() => T.RenderOrderByClause(new SortDirective(new List<SortDirectiveItem>()), RendererMaps.Standard, D.Prefix)).ParamName);
            Assert.Equal("fieldToColumnMap", Assert.Throws<ArgumentNullException>(() =>
                T.RenderOrderByClause(
                    new SortDirective(new List<SortDirectiveItem> { new() { Expression = new DummyBooleanFunction(), Direction = SortDirection.Ascending } }),
                    (Dictionary<string, string>)null!,
                    D.Prefix)).ParamName);
        }

        private static FilterCriteria EqName(string value) =>
            new() { Expression = new EqFunc(new Field("name", typeof(string)), new Literal(value)) };
    }
}
