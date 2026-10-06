namespace Expresso.Rendering.EntityFramework.Test
{
    /// <summary>
    /// Scenario: child rows are sorted with ORDER BY partition_key, then nested sort keys (same pattern as sample NestedSortLoader).
    /// Setup: two authors with awards in arbitrary order. Expect: each author's awards are title-ordered after grouping.
    /// </summary>
    public sealed class PartitionedChildOrderTests
    {
        [Fact]
        public void Partition_then_sort_preserves_per_parent_order()
        {
            var rows = new (int AuthorId, string Title)[]
            {
                (2, "z"),
                (1, "b"),
                (1, "a"),
                (2, "m"),
            };

            var sorted = rows
                .OrderBy(r => r.AuthorId)
                .ThenBy(r => r.Title, StringComparer.Ordinal)
                .ToList();

            var byAuthor = new Dictionary<int, List<string>>();
            foreach (var row in sorted)
            {
                if (!byAuthor.TryGetValue(row.AuthorId, out var list))
                {
                    list = new List<string>();
                    byAuthor[row.AuthorId] = list;
                }

                list.Add(row.Title);
            }

            Assert.Equal(new[] { "a", "b" }, byAuthor[1]);
            Assert.Equal(new[] { "m", "z" }, byAuthor[2]);
        }
    }
}
