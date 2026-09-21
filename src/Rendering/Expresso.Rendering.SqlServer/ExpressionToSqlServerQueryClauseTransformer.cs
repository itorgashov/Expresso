namespace Expresso.Rendering
{
    /// <summary>
    /// SQL Server renderer. Uses default walker hooks (bracket quoting, <c>@</c> parameters, T-SQL functions).
    /// </summary>
    public sealed class ExpressionToSqlServerQueryClauseTransformer : ExpressionToSqlQueryClauseTransformerBase
    {
    }
}
