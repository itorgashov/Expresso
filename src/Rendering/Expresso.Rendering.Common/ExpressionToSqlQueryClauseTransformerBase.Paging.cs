using Expresso.Core.Paging;

namespace Expresso.Rendering
{
    public abstract partial class ExpressionToSqlQueryClauseTransformerBase
    {
        /// <inheritdoc />
        public (string pagingClause, Dictionary<string, object> parameters) RenderPagingClause(PagingDirective paging, string paramNamePrefix)
        {
            if (paging is null)
            {
                throw new ArgumentNullException(nameof(paging));
            }

            EnsureParamNamePrefix(paramNamePrefix);
            if (paging.IsEmpty)
            {
                return (string.Empty, new Dictionary<string, object>());
            }

            var parameters = new Dictionary<string, object>();
            var offsetName = AddParameter(paging.Offset, parameters, paramNamePrefix);
            string? limitName = null;
            if (paging.Limit is int limit)
            {
                limitName = AddParameter(limit, parameters, paramNamePrefix);
            }

            return (FormatPagingClause(offsetName, limitName), parameters);
        }

        /// <summary>
        /// Formats a paging clause from already-bound parameter names.
        /// The default is <c>OFFSET … ROWS FETCH NEXT … ROWS ONLY</c> (SQL Server, PostgreSQL, Oracle 12c+, DB2 11.1+).
        /// </summary>
        /// <param name="offsetParameter">Bind name of the offset. Always present.</param>
        /// <param name="limitParameter">Bind name of the limit, or <see langword="null"/> when the caller reads through the end.</param>
        /// <returns>The clause, including keywords.</returns>
        protected virtual string FormatPagingClause(string offsetParameter, string? limitParameter) =>
            limitParameter is null
                ? $"OFFSET {offsetParameter} ROWS"
                : $"OFFSET {offsetParameter} ROWS FETCH NEXT {limitParameter} ROWS ONLY";
    }
}
