using Expresso.Core.Filtering;
using Expresso.Core.Projection;
using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Parsing.Test;

public sealed class ProjectionDirectiveParserTests
{
    private static readonly QueryModel Model = new QueryModel(new[] { ("title", typeof(string)), ("year", typeof(int)) }, new[] {
        new CollectionModel("authors", new QueryModel(new[] { ("lastname", typeof(string)) }, new[] {
            new CollectionModel("awards", new QueryModel(new[] { ("title", typeof(string)) })) })) });
    private readonly ProjectionDirectiveParser _parser = new ProjectionDirectiveParser();

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(",")]
    [InlineData("title,")]
    [InlineData(",title")]
    [InlineData("title,,year")]
    [InlineData("missing")]
    [InlineData("authors.missing")]
    [InlineData("authors..lastname")]
    [InlineData(".title")]
    [InlineData("title.")]
    [InlineData("title.year")]
    [InlineData("authors.lastname.extra")]
    [InlineData("authors,authors.missing")]
    public void MalformedOrUnknown_IsRejected(string fields) =>
        Assert.Throws<ArgumentException>(() => _parser.Parse(fields, Model));

    [Fact]
    public void MissingSelectsAll_AndModelIsRequired()
    {
        var all = _parser.Parse(null, Model);
        Assert.False(all.IsAll);
        Assert.Equal(new[] { "authors", "title", "year" }, all.Selections.Select(s => s.Name));
        Assert.Throws<ArgumentNullException>(() => _parser.Parse(null, null!));
        using var services = new ServiceCollection().AddRequestParametersParsers().BuildServiceProvider();
        Assert.IsType<ProjectionDirectiveParser>(services.GetRequiredService<IProjectionDirectiveParser>());
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddRequestParametersParsers((Action<LiteralParseOptions>)null!));
        var configured = false;
        using var configuredServices = new ServiceCollection().AddRequestParametersParsers(_ => configured = true).BuildServiceProvider();
        Assert.True(configured);
        Assert.Equal("title", Assert.Single(configuredServices.GetRequiredService<IProjectionDirectiveParser>().Parse("title", Model).Selections).Name);
    }

    [Fact]
    public void WhitespaceCaseAndDuplicates_AreNormalized()
    {
        var result = _parser.Parse(" YEAR , title, Title, authors . lastname , authors.awards.title ", Model);
        Assert.Equal(new[] { "authors", "title", "year" }, result.Selections.Select(s => s.Name));
        Assert.Equal(new[] { "awards", "lastname" }, result.Find("authors")!.Children.Select(s => s.Name));
        Assert.Equal("title", result.Find("authors")!.Children[0].Children[0].Name);
    }

    [Theory]
    [InlineData("authors,authors.lastname")]
    [InlineData("authors.lastname,authors")]
    [InlineData("authors.awards.title,authors,authors.lastname")]
    public void BroaderPath_SubsumesNarrower(string fields)
    {
        var result = _parser.Parse(fields, Model);
        var authors = Assert.Single(result.Selections);
        Assert.False(authors.SelectDefaultSubtree);
        Assert.Equal(new[] { "awards", "lastname" }, authors.Children.Select(s => s.Name));
    }

    [Fact]
    public void NestedDefault_SubsumesItsChildrenOnly()
    {
        var result = _parser.Parse("authors.awards.title,authors.lastname,authors.awards", Model);
        var authors = Assert.Single(result.Selections);
        Assert.False(authors.SelectDefaultSubtree);
        Assert.False(authors.Children[0].SelectDefaultSubtree);
        Assert.Equal("title", Assert.Single(authors.Children[0].Children).Name);
        Assert.Equal("lastname", authors.Children[1].Name);
    }
}
