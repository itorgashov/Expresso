namespace Expresso.Rendering.Linq
{
    /// <summary>
    /// Queryable profile: lambdas use BCL members that LINQ providers translate to SQL, so function semantics
    /// (collation, rounding, <c>LEN</c>, …) are the database's. Use with <see cref="System.Linq.IQueryable{T}"/>.
    /// </summary>
    public class QueryableExpressionToLinqTransformer : ExpressionToLinqTransformerBase
    {
    }
}
