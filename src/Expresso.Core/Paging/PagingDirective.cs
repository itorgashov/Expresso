namespace Expresso.Core.Paging
{
    /// <summary>
    /// Optional row window for a list query. Page and page size win over skip and take when both styles are set.
    /// </summary>
    public sealed class PagingDirective
    {
        /// <summary>No paging: offset 0 and no limit.</summary>
        public static PagingDirective None { get; } = new PagingDirective();

        /// <summary>Creates a directive from the raw query values. Ignored values are still validated.</summary>
        /// <param name="page">1-based page number, or <see langword="null"/> when the client omitted it.</param>
        /// <param name="pageSize">Page size, or <see langword="null"/> when the client omitted it. When set, paging is page-based and <paramref name="page"/> defaults to 1.</param>
        /// <param name="skip">Rows to skip, or <see langword="null"/> when the client omitted it. Used only when <paramref name="pageSize"/> is not set.</param>
        /// <param name="take">Maximum rows to return, or <see langword="null"/> to read through the end. Used only when <paramref name="pageSize"/> is not set.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="page"/>, <paramref name="pageSize"/>, or <paramref name="take"/> is less than 1, <paramref name="skip"/> is negative, or the computed offset does not fit in an <see cref="int"/>.</exception>
        public PagingDirective(int? page = null, int? pageSize = null, int? skip = null, int? take = null)
        {
            RequireAtLeast(page, 1, nameof(page));
            RequireAtLeast(pageSize, 1, nameof(pageSize));
            RequireAtLeast(skip, 0, nameof(skip));
            RequireAtLeast(take, 1, nameof(take));

            Page = page;
            PageSize = pageSize;
            Skip = skip;
            Take = take;
            IsPageBased = pageSize.HasValue;
            HasPageAndSkipTake = (page.HasValue || pageSize.HasValue) && (skip.HasValue || take.HasValue);

            if (IsPageBased)
            {
                var effectivePage = page ?? 1;
                var offset = (long)(effectivePage - 1) * pageSize!.Value;
                if (offset > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(page), page, "The page offset is larger than " + int.MaxValue + ".");
                }

                Offset = (int)offset;
                Limit = pageSize;
            }
            else
            {
                Offset = skip ?? 0;
                Limit = take;
            }
        }

        /// <summary>Raw page number, including a value the renderer ignores because <see cref="PageSize"/> is missing.</summary>
        public int? Page { get; }

        /// <summary>Raw page size. When set, <see cref="Offset"/> and <see cref="Limit"/> come from the page.</summary>
        public int? PageSize { get; }

        /// <summary>Raw skip, including a value ignored because <see cref="IsPageBased"/> is true.</summary>
        public int? Skip { get; }

        /// <summary>Raw take, including a value ignored because <see cref="IsPageBased"/> is true.</summary>
        public int? Take { get; }

        /// <summary><see langword="true"/> when <see cref="PageSize"/> is set.</summary>
        public bool IsPageBased { get; }

        /// <summary><see langword="true"/> when a page or page size was sent together with a skip or take.</summary>
        public bool HasPageAndSkipTake { get; }

        /// <summary>Rows to skip. Zero when paging is empty or the first page is requested.</summary>
        public int Offset { get; }

        /// <summary>Maximum rows to return, or <see langword="null"/> to read through the end.</summary>
        public int? Limit { get; }

        /// <summary><see langword="true"/> when nothing should be rendered: offset 0 and no limit.</summary>
        public bool IsEmpty => Offset == 0 && Limit is null;

        /// <summary>Number of pages of <paramref name="pageSize"/> needed for <paramref name="totalCount"/> rows. Zero when the count is zero.</summary>
        /// <param name="totalCount">Matching row count. Must be at least 0.</param>
        /// <param name="pageSize">Page size. Must be at least 1.</param>
        /// <returns>The ceiling of <paramref name="totalCount"/> divided by <paramref name="pageSize"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="totalCount"/> is negative or <paramref name="pageSize"/> is less than 1.</exception>
        public static long TotalPages(long totalCount, int pageSize)
        {
            if (totalCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalCount), totalCount, "totalCount must be at least 0.");
            }

            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "pageSize must be at least 1.");
            }

            if (totalCount == 0)
            {
                return 0;
            }

            return (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);
        }

        private static void RequireAtLeast(int? value, int minimum, string name)
        {
            if (value is int actual && actual < minimum)
            {
                throw new ArgumentOutOfRangeException(name, actual, name + " must be at least " + minimum + ".");
            }
        }
    }
}
