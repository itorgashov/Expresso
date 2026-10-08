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

        [Fact]
        public void ThenBy_AppendsKeyAndKeepsNested()
        {
            var year = new Field("year", typeof(int));
            var nested = new CollectionSort(
                "authors",
                new SortDirective(new[]
                {
                    new SortDirectiveItem { Expression = new Field("lastname", typeof(string), "authors"), Direction = SortDirection.Ascending },
                }));
            var directive = new SortDirective(
                new[] { new SortDirectiveItem { Expression = year, Direction = SortDirection.Descending } },
                new[] { nested });
            var id = new Field("id", typeof(int));

            var extended = directive.ThenBy(id);

            Assert.Single(directive.Items);
            Assert.Equal(2, extended.Items.Count);
            Assert.Same(year, extended.Items[0].Expression);
            Assert.Equal(SortDirection.Descending, extended.Items[0].Direction);
            Assert.Same(id, extended.Items[1].Expression);
            Assert.Equal(SortDirection.Ascending, extended.Items[1].Direction);
            Assert.Single(extended.Nested);
            Assert.Same(directive.Nested[0], extended.Nested[0]);
        }

        [Fact]
        public void ThenBy_RejectsNullExpression()
        {
            var directive = new SortDirective(Array.Empty<SortDirectiveItem>());
            Assert.Throws<ArgumentNullException>(() => directive.ThenBy(null!));
        }
    }
}
