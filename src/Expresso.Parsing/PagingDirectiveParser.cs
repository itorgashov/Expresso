using System.Globalization;
using Expresso.Core.Paging;

namespace Expresso.Parsing
{
    /// <summary>Parses paging query values with invariant, digits-only integers.</summary>
    public sealed class PagingDirectiveParser : IPagingDirectiveParser
    {
        /// <inheritdoc />
        public PagingDirective Parse(string? page, string? pageSize, string? skip, string? take) =>
            new PagingDirective(
                ParseOptional(page, "page"),
                ParseOptional(pageSize, "pagesize"),
                ParseOptional(skip, "skip"),
                ParseOptional(take, "take"));

        private static int? ParseOptional(string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
            {
                throw new ArgumentException($"'{name}' must be a whole number: '{value}'.", name);
            }

            return parsed;
        }
    }
}
