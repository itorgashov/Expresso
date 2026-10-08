using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Sorting;

namespace Expresso.Rendering.TestCases
{
    public sealed record PagingCase(string Id, PagingDirective Paging, int[] ExpectedIdsOrdered, SortDirective? Sort = null, FilterCriteria? Filter = null);

    public static partial class RendererIntegrationCases
    {
        public static IEnumerable<object[]> PagingCases() =>
            AllPaging().Select(c => new object[] { c });

        public static IReadOnlyList<PagingCase> AllPaging()
        {
            var name = new Field("name", typeof(string));
            var age = new Field("age", typeof(int));
            var tags = new CollectionRef("tags");
            var nameAsc = S(name, SortDirection.Ascending);
            var ageDescName = new SortDirective(new List<SortDirectiveItem>
            {
                new() { Expression = age, Direction = SortDirection.Descending },
                new() { Expression = name, Direction = SortDirection.Ascending },
            });
            var countTags = new SortDirective(new List<SortDirectiveItem>
            {
                new() { Expression = new CollectionCountFunc(tags), Direction = SortDirection.Descending },
                new() { Expression = name, Direction = SortDirection.Ascending },
            });

            return new List<PagingCase>
            {
                new("page-1-size-2", new PagingDirective(page: 1, pageSize: 2), Ids(1, 2), nameAsc),
                new("page-2-size-2", new PagingDirective(page: 2, pageSize: 2), Ids(3, 4), nameAsc),
                new("page-2-size-4", new PagingDirective(page: 2, pageSize: 4), Ids(5, 6), nameAsc),
                new("page-4-size-2", new PagingDirective(page: 4, pageSize: 2), Ids(), nameAsc),
                new("pagesize-only-3", new PagingDirective(pageSize: 3), Ids(1, 2, 3)),
                new("skip-2-take-3", new PagingDirective(skip: 2, take: 3), Ids(3, 4, 5)),
                new("skip-only-4", new PagingDirective(skip: 4), Ids(5, 6)),
                new("take-only-2", new PagingDirective(take: 2), Ids(1, 2)),
                new("skip-beyond", new PagingDirective(skip: 10), Ids()),
                new("page-wins", new PagingDirective(page: 2, pageSize: 2, skip: 0, take: 5), Ids(3, 4), nameAsc),
                new("page-without-size", new PagingDirective(page: 3, skip: 1, take: 2), Ids(2, 3)),
                new("page-only", new PagingDirective(page: 3), Ids(1, 2, 3, 4, 5, 6)),
                new("page-age-desc", new PagingDirective(page: 2, pageSize: 2), Ids(3, 2), ageDescName),
                new("page-filter", new PagingDirective(page: 2, pageSize: 2), Ids(4), nameAsc, F(new GteFunc(age, new Literal(30)))),
                new("page-count-tags", new PagingDirective(page: 2, pageSize: 3), Ids(5, 6, 3), countTags),
            };
        }
    }
}
