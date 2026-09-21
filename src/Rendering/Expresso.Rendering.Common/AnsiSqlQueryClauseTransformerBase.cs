using System;
using System.Linq;

namespace Expresso.Rendering
{
    /// <summary>
    /// Shared quoting, concat, and length defaults for PostgreSQL, SQLite, Oracle, and DB2.
    /// </summary>
    public abstract class AnsiSqlQueryClauseTransformerBase : ExpressionToSqlQueryClauseTransformerBase
    {
        /// <inheritdoc />
        /// <remarks>
        /// Map values may already be dialect-qualified fragments (e.g. Oracle <c>a."first_name"</c>).
        /// Re-quoting those splits on <c>.</c> inside quotes and yields ORA-01741-style zero-length identifiers.
        /// </remarks>
        protected override string QuoteIdentifier(string input)
        {
            if (input.IndexOf('"') >= 0)
            {
                return input;
            }

            var parts = input.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(".", parts.Select(QuoteIdentifierPart));
        }

        protected override string QuoteIdentifierPart(string part) =>
            "\"" + part.Replace("\"", "\"\"") + "\"";

        protected override string LengthFunctionName => "LENGTH";

        protected override string SqlStringConcatOperator => " || ";
    }
}
