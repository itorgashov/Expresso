using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Sorting;

namespace Expresso.Tests.Core.Sorting
{
    public class SortDirectiveTests
    {
        [Fact]
        public void Constructor_OneArg_LeavesNestedEmpty()
        {
            var directive = new SortDirective(new[]
            {
                new SortDirectiveItem { Expression = new Field("year", typeof(int)), Direction = SortDirection.Descending },
            });

            Assert.Empty(directive.Nested);
        }

        [Fact]
        public void Constructor_TwoArg_PreservesNested()
        {
            var nested = new[]
            {
                new CollectionSort(
                    "authors",
                    new SortDirective(new[]
                    {
                        new SortDirectiveItem
                        {
                            Expression = new Field("lastname", typeof(string), "authors"),
                            Direction = SortDirection.Ascending,
                        },
                    })),
            };

            var directive = new SortDirective(Array.Empty<SortDirectiveItem>(), nested);

            Assert.Empty(directive.Items);
            Assert.Single(directive.Nested);
            Assert.Equal("authors", directive.Nested[0].Name);
            Assert.Single(directive.Nested[0].Directive.Items);
        }

        [Fact]
        public void RemoveDuplicates_PreservesNestedAndDoesNotMixScopes()
        {
            var year = new Field("year", typeof(int));
            var lastname = new Field("lastname", typeof(string), "authors");
            var directive = new SortDirective(
                new[]
                {
                    new SortDirectiveItem { Expression = year, Direction = SortDirection.Descending },
                    new SortDirectiveItem { Expression = year, Direction = SortDirection.Ascending },
                },
                new[]
                {
                    new CollectionSort(
                        "authors",
                        new SortDirective(
                            new[]
                            {
                                new SortDirectiveItem { Expression = lastname, Direction = SortDirection.Ascending },
                                new SortDirectiveItem { Expression = lastname, Direction = SortDirection.Descending },
                            },
                            Array.Empty<CollectionSort>())),
                });

            var deduped = directive.RemoveDuplicates();

            Assert.Equal(4, directive.TotalSortKeyCount());
            Assert.Equal(2, deduped.TotalSortKeyCount());
            Assert.Single(deduped.Items);
            Assert.Single(deduped.Nested);
            Assert.Single(deduped.Nested[0].Directive.Items);
        }

        [Fact]
        public void TotalSortKeyCount_IncludesNestedItems()
        {
            var directive = new SortDirective(
                new[]
                {
                    new SortDirectiveItem { Expression = new Field("year", typeof(int)), Direction = SortDirection.Descending },
                },
                new[]
                {
                    new CollectionSort(
                        "authors",
                        new SortDirective(
                            new[]
                            {
                                new SortDirectiveItem
                                {
                                    Expression = new Field("lastname", typeof(string), "authors"),
                                    Direction = SortDirection.Ascending,
                                },
                            },
                            new[]
                            {
                                new CollectionSort(
                                    "awards",
                                    new SortDirective(new[]
                                    {
                                        new SortDirectiveItem
                                        {
                                            Expression = new Field("title", typeof(string), "authors.awards"),
                                            Direction = SortDirection.Descending,
                                        },
                                    })),
                            })),
                });

            Assert.Equal(3, directive.TotalSortKeyCount());
        }
    }
}
