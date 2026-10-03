namespace Expresso.Rendering.Linq.Test
{
    /// <summary>Every function page documents the SQL, LINQ, EF Core and EF6 rendering, in that order.</summary>
    public class FunctionPageTests
    {
        private static readonly string[] Headings = { "## SQL rendering", "## LINQ rendering", "## EF Core rendering", "## EF6 rendering" };

        private static readonly string FunctionsDirectory = Path.Combine(RepositoryRoot(), "docs", "functions");

        public static IEnumerable<object[]> Pages() =>
            Directory.EnumerateFiles(FunctionsDirectory, "*.md", SearchOption.AllDirectories)
                .Where(path => Path.GetFileName(path) != "README.md")
                .Select(path => new object[] { path.Substring(FunctionsDirectory.Length + 1) });

        [Theory]
        [MemberData(nameof(Pages))]
        public void Page_HasEveryRenderingSectionInOrder(string page)
        {
            var lines = File.ReadAllLines(Path.Combine(FunctionsDirectory, page)).Select(line => line.TrimEnd()).ToList();
            var positions = Headings.Select(heading => lines.IndexOf(heading)).ToList();

            Assert.All(Headings.Zip(positions, (heading, position) => (heading, position)), h => Assert.True(h.position >= 0, $"Missing '{h.heading}'."));
            Assert.Equal(positions.OrderBy(p => p), positions);
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Expresso.slnx")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("Expresso.slnx not found above " + AppContext.BaseDirectory);
        }
    }
}
