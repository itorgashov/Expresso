using System.Text;

namespace Expresso.Rendering
{
    /// <summary>
    /// SQL Server renderer. Uses default walker hooks (bracket quoting, <c>@</c> parameters, T-SQL functions).
    /// <c>[</c> is escaped in <c>LIKE</c> patterns so a bracket is a literal character, not a character class.
    /// </summary>
    public sealed class ExpressionToSqlServerQueryClauseTransformer : ExpressionToSqlQueryClauseTransformerBase
    {
        /// <inheritdoc />
        protected override string EscapeLikeLiteral(string value) =>
            base.EscapeLikeLiteral(value).Replace("[", "\\[");

        /// <inheritdoc />
        protected override void AppendLikeEscapedExpression(Action emitInner, StringBuilder sqlBuilder)
        {
            sqlBuilder.Append("REPLACE(REPLACE(REPLACE(REPLACE(");
            emitInner();
            sqlBuilder.Append(", '\\', '\\\\'), '%', '\\%'), '_', '\\_'), '[', '\\[')");
        }
    }
}
