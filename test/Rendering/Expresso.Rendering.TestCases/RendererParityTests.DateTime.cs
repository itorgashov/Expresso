using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.Rendering.TestCases
{
    public abstract partial class RendererParityTests
    {
        [Fact]
        public void GenerateWhereClause_DateGetters_ReturnDialectSql()
        {
            var col = D.Q("b.created_at");
            Assert.Equal($"({D.Year(col)} = {D.P(0)})", RenderDateEqInt(new YearFunc(CreatedAt())).whereClause);
            Assert.Equal($"({D.Month(col)} = {D.P(0)})", RenderDateEqInt(new MonthFunc(CreatedAt())).whereClause);
            Assert.Equal($"({D.Day(col)} = {D.P(0)})", RenderDateEqInt(new DayFunc(CreatedAt())).whereClause);
            Assert.Equal($"({D.DayOfYear(col)} = {D.P(0)})", RenderDateEqInt(new DayOfYearFunc(CreatedAt())).whereClause);
            Assert.Equal($"({D.Hour(col)} = {D.P(0)})", RenderDateEqInt(new HourFunc(CreatedAt())).whereClause);
            Assert.Equal($"({D.DayOfWeek(col)} = {D.P(0)})", RenderDateEqInt(new DayOfWeekFunc(CreatedAt()), 0).whereClause);
            Assert.Equal($"({D.DateCast(col)} = {D.P(0)})",
                T.RenderWhereClause(
                    new()
                    {
                        Expression = new EqFunc(
                            new DateFunc(CreatedAt()),
#if NET6_0_OR_GREATER
                            new Literal(new DateOnly(2020, 1, 1)))
#else
                            new Literal(new DateTime(2020, 1, 1)))
#endif
                    },
                    RendererMaps.DateTime,
                    D.Prefix).whereClause);
        }

        [Fact]
        public void GenerateWhereClause_DateAdd_ReturnsDialectSql()
        {
            var col = D.Q("b.created_at");
            var result = T.RenderWhereClause(
                new() { Expression = new EqFunc(new AddYearsFunc(CreatedAt(), new Literal(1)), CreatedAt()) },
                RendererMaps.DateTime,
                D.Prefix);
            Assert.Equal($"({D.DateAdd("year", D.P(0), col)} = {col})", result.whereClause);
            Assert.Equal(1, result.parameters[D.P(0)]);
        }

        [Fact]
        public void GenerateOrderBy_Year_ReturnsYearSql()
        {
            var sort = new SortDirective(new List<SortDirectiveItem>
            {
                new() { Expression = new YearFunc(CreatedAt()), Direction = SortDirection.Ascending }
            });
            Assert.Equal($"{D.Year(D.Q("b.created_at"))} ASC", T.RenderOrderByClause(sort, RendererMaps.DateTime, D.Prefix).orderByClause);
        }

        [Fact]
        public void GenerateWhereClause_GuidEq_ReturnsParameterizedSql()
        {
            var guid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
            var result = T.RenderWhereClause(
                new() { Expression = new EqFunc(new Field("id", typeof(Guid)), new Literal(guid)) },
                RendererMaps.DateTime,
                D.Prefix);
            Assert.Equal($"({D.Q("b.id")} = {D.P(0)})", result.whereClause);
            Assert.Equal(guid, result.parameters[D.P(0)]);
        }

        private static Field CreatedAt() => new("createdat", typeof(DateTime));

        private (string whereClause, Dictionary<string, object> parameters) RenderDateEqInt(AbstractExpression left, int right = 2020)
        {
            FilterCriteria filter = new() { Expression = new EqFunc(left, new Literal(right)) };
            return T.RenderWhereClause(filter, RendererMaps.DateTime, D.Prefix);
        }
    }
}
