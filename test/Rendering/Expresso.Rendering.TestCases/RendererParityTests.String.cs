using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.Rendering.TestCases
{
    public abstract partial class RendererParityTests
    {
        [Fact]
        public void GenerateWhereClause_StringPredicates_ReturnLikeWithEscape()
        {
            var starts = T.RenderWhereClause(
                new() { Expression = new StrStartswithFunc(new Field("name", typeof(string)), new Literal("Jo")) },
                RendererMaps.Standard,
                D.Prefix);
            Assert.Equal($"({D.Q("name_col")} LIKE {D.P(0)} {D.LikeEscape})", starts.whereClause);
            Assert.Equal("Jo%", starts.parameters[D.P(0)]);

            var contains = T.RenderWhereClause(
                new() { Expression = new StrContainsFunc(new Field("name", typeof(string)), new Literal("100%")) },
                RendererMaps.Standard,
                D.Prefix);
            Assert.Equal("%100\\%%", contains.parameters[D.P(0)]);
        }

        [Fact]
        public void GenerateWhereClause_StringTransforms_ReturnDialectSql()
        {
            var name = D.Q("name_col");
            Assert.Equal($"({D.Left(name, D.P(0))} = {D.P(1)})",
                T.RenderWhereClause(new() { Expression = new EqFunc(new LeftFunc(new Field("name", typeof(string)), new Literal(1)), new Literal("J")) }, RendererMaps.Standard, D.Prefix).whereClause);
            Assert.Equal($"({D.Length(name)} = {D.P(0)})",
                T.RenderWhereClause(new() { Expression = new EqFunc(new LenFunc(new Field("name", typeof(string))), new Literal(4)) }, RendererMaps.Standard, D.Prefix).whereClause);
            Assert.Equal($"({D.Lower(name)} = {D.P(0)})",
                T.RenderWhereClause(new() { Expression = new EqFunc(new LowerFunc(new Field("name", typeof(string))), new Literal("john")) }, RendererMaps.Standard, D.Prefix).whereClause);
            Assert.Equal($"({D.Substring(name, D.P(0), D.P(1))} = {D.P(2)})",
                T.RenderWhereClause(new() { Expression = new EqFunc(new SubStringFunc(new Field("name", typeof(string)), new Literal(1), new Literal(3)), new Literal("John")) }, RendererMaps.Standard, D.Prefix).whereClause);

            var concat = T.RenderWhereClause(
                new()
                {
                    Expression = new EqFunc(
                        new ConcatFunc(new List<AbstractExpression>
                        {
                            new Field("name", typeof(string)),
                            new Literal(" "),
                            new Field("foo", typeof(string))
                        }),
                        new Literal("John Doe"))
                },
                RendererMaps.Standard,
                D.Prefix);
            Assert.Equal($"({D.Concat(name, D.P(0), D.Q("foo_col"))} = {D.P(1)})", concat.whereClause);
        }

        [Fact]
        public void GenerateWhereClause_IndexOf_ReturnsZeroBasedSql()
        {
            var result = T.RenderWhereClause(
                new()
                {
                    Expression = new GtFunc(
                        new IndexOfFunc(new Field("name", typeof(string)), new Literal("o")),
                        new Literal(0))
                },
                RendererMaps.Standard,
                D.Prefix);
            Assert.Equal($"({D.IndexOf(D.Q("name_col"), D.P(0))} > {D.P(1)})", result.whereClause);
        }

        [Fact]
        public void GenerateOrderBy_Lower_ReturnsLowerSql()
        {
            var sort = new SortDirective(new List<SortDirectiveItem>
            {
                new() { Expression = new LowerFunc(new Field("name", typeof(string))), Direction = SortDirection.Ascending }
            });
            Assert.Equal($"{D.Lower(D.Q("name_col"))} ASC", T.RenderOrderByClause(sort, RendererMaps.Standard, D.Prefix).orderByClause);
        }
    }
}
