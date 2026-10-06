using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Rendering;

namespace Expresso.Tests.SqlServer
{
    public class SqlServerLikeEscapeTests
    {
        [Fact]
        public void Brackets_AreLiteralCharacters()
        {
            // Scenario: SQL Server LIKE treats [ as a character class. A search for the characters [ab] must escape it.
            var contains = Render(new StrContainsFunc(new Field("name", typeof(string)), new Literal("[ab]")));
            var range = Render(new StrContainsFunc(new Field("name", typeof(string)), new Literal("[a-c]")));
            var prefix = Render(new StrStartswithFunc(new Field("name", typeof(string)), new Literal("[Bb]")));
            var suffix = Render(new StrEndswithFunc(new Field("name", typeof(string)), new Literal("[ab]")));
            Assert.Equal(@"%\[ab]%", contains);
            Assert.Equal(@"%\[a-c]%", range);
            Assert.Equal(@"\[Bb]%", prefix);
            Assert.Equal(@"%\[ab]", suffix);
        }

        [Fact]
        public void EscapedCharacters_StayEscapedWhenBracketsAreAdded()
        {
            var parameters = Render(new StrContainsFunc(new Field("name", typeof(string)), new Literal(@"100%_[a")));
            Assert.Equal(@"%100\%\_\[a%", parameters);
        }

        [Fact]
        public void ColumnPattern_EscapesTheOpeningBracket()
        {
            var transformer = new ExpressionToSqlServerQueryClauseTransformer();
            var (sql, _) = transformer.RenderWhereClause(
                new FilterCriteria { Expression = new StrContainsFunc(new Field("name", typeof(string)), new Field("notes", typeof(string))) },
                new Dictionary<string, string> { ["name"] = "[name]", ["notes"] = "[notes]" },
                "p");
            Assert.Contains("'[', '\\['", sql);
        }

        private static string Render(BooleanFunction filter)
        {
            var transformer = new ExpressionToSqlServerQueryClauseTransformer();
            var (_, parameters) = transformer.RenderWhereClause(
                new FilterCriteria { Expression = filter },
                new Dictionary<string, string> { ["name"] = "[name]" },
                "p");
            return parameters.Values.Single().ToString()!;
        }
    }
}
