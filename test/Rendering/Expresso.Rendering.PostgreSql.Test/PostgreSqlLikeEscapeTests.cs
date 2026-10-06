using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Filtering;
using Expresso.Rendering;

namespace Expresso.Tests.PostgreSql
{
    public class PostgreSqlLikeEscapeTests
    {
        [Fact]
        public void Brackets_StayUnescaped()
        {
            // Scenario: PostgreSQL LIKE does not treat [ as a character class, so the bracket is already literal.
            var transformer = new ExpressionToPostgreSqlQueryClauseTransformer();
            var (_, parameters) = transformer.RenderWhereClause(
                new FilterCriteria { Expression = new StrContainsFunc(new Field("name", typeof(string)), new Literal("[ab]")) },
                new Dictionary<string, string> { ["name"] = "\"name\"" },
                "p");
            Assert.Equal("%[ab]%", parameters.Values.Single().ToString());
        }
    }
}
