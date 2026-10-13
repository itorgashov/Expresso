using Expresso.Core.Filtering;

namespace Expresso.Parsing.Test;

public sealed class ProjectionUnicodeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnicodeEquivalentPaths_PreserveCatalogLookupAndDeduplication(bool uppercase)
    {
        const string name = "\u03ba\u03cc\u03c3\u03bc\u03bf\u03c2";
        var requested = uppercase ? name.ToUpperInvariant() : name.Substring(0, name.Length - 1) + "\u03c3";
        var child = new QueryModel(new[] { (name, typeof(string)) });
        var model = new QueryModel(new[] { (name, typeof(string)) }, new[] { new CollectionModel("children" + name, child) });
        if (!string.Equals(name, requested, StringComparison.OrdinalIgnoreCase))
        {
            Assert.Throws<ArgumentException>(() => new ProjectionDirectiveParser().Parse(requested, model));
            return;
        }
        var result = new ProjectionDirectiveParser().Parse(requested + "," + name + ",children" + requested + "." + requested, model);
        Assert.Equal(2, result.Selections.Count);
        Assert.NotNull(result.Find(name));
        Assert.NotNull(result.Find("children" + name)!.ToDirective().Find(name));
        Assert.Throws<ArgumentException>(() => new ProjectionDirectiveParser().Parse(name + "x", model));
    }
}
