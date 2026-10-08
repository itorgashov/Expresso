using Expresso.Core.Paging;

namespace Expresso.Parsing
{
    /// <summary>Parses the optional <c>page</c>, <c>pagesize</c>, <c>skip</c>, and <c>take</c> query values.</summary>
    public interface IPagingDirectiveParser
    {
        /// <summary>Parses the four raw query values. A null or whitespace value counts as omitted.</summary>
        /// <param name="page">Raw <c>page</c> value.</param>
        /// <param name="pageSize">Raw <c>pagesize</c> value.</param>
        /// <param name="skip">Raw <c>skip</c> value.</param>
        /// <param name="take">Raw <c>take</c> value.</param>
        /// <returns>The directive. Page and page size win over skip and take when both styles are present.</returns>
        /// <exception cref="ArgumentException">A present value is not a whole number.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A value is outside the range <see cref="PagingDirective"/> accepts.</exception>
        PagingDirective Parse(string? page, string? pageSize, string? skip, string? take);
    }
}
