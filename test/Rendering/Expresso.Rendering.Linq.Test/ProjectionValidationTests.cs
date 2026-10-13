using System.Linq.Expressions;
using Expresso.Core.Projection;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Sorting;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.Linq.Test;

public sealed class ProjectionValidationTests
{
    [Fact]
    public void CyclicMappingsAreRejected_ButSharedSubtreesAreAllowed()
    {
        var query = new LinqQueryMapping<TreeEntity>().Field("id", x => x.Id);
        var first = new LinqProjectionMapping<TreeEntity, TreeDto>().Key("id", x => x.Id);
        var second = new LinqProjectionMapping<TreeEntity, TreeDto>().Key("id", x => x.Id);
        Assert.Throws<ArgumentException>(() => first.Collection("self", "self", x => x.Children, x => x.Children, first, query));
        first.Collection("children", "children", x => x.Children, x => x.Children, second, query);
        Assert.Throws<ArgumentException>(() => second.Collection("back", "back", x => x.Children, x => x.Children, first, query));
        var third = new LinqProjectionMapping<TreeEntity, TreeDto>().Key("id", x => x.Id)
            .Collection("children", "children", x => x.Children, x => x.Children, first, query);
        third.Collection("shared", "shared", x => x.Children, x => x.OtherChildren, second, query);
        Assert.Equal(2, third.GetProjectionModel().Collections.Length);
    }

    private static LinqProjectionMapping<ProjectionChild, ProjectionChildDto> Child() =>
        new LinqProjectionMapping<ProjectionChild, ProjectionChildDto>().Key("id", x => x.Id).Field("name", "name", x => x.Name, x => x.Name);
    private static LinqQueryMapping<ProjectionChild> Query() => new LinqQueryMapping<ProjectionChild>().Field("name", x => x.Name);
    private static LinqProjectionMapping<ProjectionParent, ProjectionParentDto> Parent() =>
        new LinqProjectionMapping<ProjectionParent, ProjectionParentDto>().Key("id", x => x.Id);

    [Fact]
    public void FieldAndCollectionContractsRejectInvalidConfiguration()
    {
        Assert.Throws<ArgumentException>(() => Parent().Field("", "title", x => x.Title, x => x.Title));
        Assert.Throws<ArgumentException>(() => Parent().Field("title", "", x => x.Title, x => x.Title));
        Assert.Throws<ArgumentNullException>(() => Parent().Field<string>("title", "title", null!, x => x.Title));
        Assert.Throws<ArgumentNullException>(() => Parent().Field<string>("title", "title", x => x.Title, null!));
        Assert.Throws<ArgumentException>(() => Parent().Field("title", "title", x => x.Title, x => x.Title.ToLower()));
        Assert.Throws<ArgumentException>(() => Parent().Field("length", "length", x => x.Id, x => x.Title.Length));
        Assert.Throws<ArgumentException>(() => Parent().Field("title", "title", x => x.Title, x => x.Title).Field("TITLE", "another", x => x.Title, x => x.Title));
        Assert.Throws<ArgumentException>(() => Parent().Field("title", "title", x => x.Title, x => x.Title).Field("another", "title", x => x.Title, x => x.Title));
        Assert.Throws<ArgumentException>(() => Parent().Key("objects", x => x.Children));
        Assert.Throws<ArgumentException>(() => new LinqProjectionMapping<ProjectionParent, InvalidDto>()
            .Field("objects", "objects", x => x.Children, x => x.Entities));
        Assert.Throws<ArgumentException>(() => Parent().Field("title", "title", x => x.Title, x => x.Title)
            .Field("another", "another", x => x.Title, x => x.Title));
        Assert.Throws<ArgumentNullException>(() => Parent().Collection<ProjectionChild, ProjectionChildDto>("children", "children", null!, x => x.Children, Child(), Query()));
        Assert.Throws<ArgumentNullException>(() => Parent().Collection<ProjectionChild, ProjectionChildDto>("children", "children", x => x.Children, null!, Child(), Query()));
        Assert.Throws<ArgumentNullException>(() => Parent().Collection<ProjectionChild, ProjectionChildDto>("children", "children", x => x.Children, x => x.Children, null!, Query()));
        Assert.Throws<ArgumentNullException>(() => Parent().Collection("children", "children", x => x.Children, x => x.Children, Child(), null!));
        Assert.Throws<ArgumentException>(() => Parent().Collection("children", "children", x => x.Children, x => x.Children, Child(), Query())
            .Field("CHILDREN", "x", x => x.Title, x => x.Title));
        Assert.Throws<ArgumentException>(() => Parent().Collection("children", "children", x => x.Children, x => x.Children, Child(), Query())
            .Field("x", "children", x => x.Title, x => x.Title));
        Assert.Throws<ArgumentException>(() => Parent().Collection("children", "children", x => x.Children, x => x.Children, Child(), Query())
            .Collection("another", "another", x => x.Children, x => x.Children, Child(), Query()));
        Assert.Throws<ArgumentException>(() => new LinqProjectionMapping<ProjectionParent, InvalidDto>().Key("id", x => x.Id)
            .Collection("children", "children", x => x.Children, x => x.NullChildren, Child(), Query()));
        Assert.Throws<ArgumentException>(() => new LinqProjectionMapping<ProjectionParent, InvalidDto>().Key("id", x => x.Id)
            .Collection("children", "children", x => x.Children, x => x.ReadOnlyChildren, Child(), Query()));
        Assert.Throws<ArgumentException>(() => new LinqProjectionMapping<ProjectionParent, InvalidDto>().Field("id", "id", x => x.Id, x => x.GetOnly));
        Assert.Throws<ArgumentException>(() => new LinqProjectionMapping<ProjectionParent, InvalidDto>().Field("id", "id", x => x.Id, x => x.PrivateSetter));
        Assert.Throws<ArgumentException>(() => new LinqProjectionMapping<ProjectionParent, InvalidDto>().Field("id", "id", x => x.Id, x => x.ReadOnlyField));
        var dto = Expression.Parameter(typeof(InvalidDto));
        var writeOnly = Expression.Lambda<Func<InvalidDto, int>>(Expression.Property(dto, "WriteOnly"), dto);
        Assert.Throws<ArgumentException>(() => new LinqProjectionMapping<ProjectionParent, InvalidDto>().Field("id", "id", x => x.Id, writeOnly));
    }

    [Fact]
    public async Task PublicFieldsAndOutputConversionRemainTypedUntilShaping()
    {
        var map = new LinqProjectionMapping<ProjectionParent, InvalidDto>().Key("id", x => x.Id)
            .Field("id", "identity", x => x.Id, x => x.ValueField, id => "id:" + id);
        var plan = new QueryableExpressionToLinqTransformer().RenderProjection(ProjectionDirective.All, map);
        var result = await plan.ExecuteAsync(ProjectionFixture.Data().AsQueryable(), new MemoryProjectionExecutor());
        Assert.Equal(1, result.Items[0].ValueField);
        Assert.Equal("id:1", ProjectionShaper.ToSparseObjects(result)[0]["identity"]);
    }

    [Fact]
    public async Task HeterogeneousThreeComponentKeysAndBinaryScalarsRemainTyped()
    {
        // Scenario: identities combine Guid, string, and integer components; repeated integer Ids remain distinct.
        var firstGuid = Guid.NewGuid();
        var secondGuid = Guid.NewGuid();
        var data = new[] {
            new HeterogeneousParent { Tenant = firstGuid, Region = "north", Id = 1, Blob = new byte[] { 1, 2 }, Children = ProjectionFixture.Data()[0].Children },
            new HeterogeneousParent { Tenant = secondGuid, Region = "south", Id = 1, Blob = new byte[] { 3 }, Children = ProjectionFixture.Data()[1].Children } };
        var map = new LinqProjectionMapping<HeterogeneousParent, HeterogeneousDto>()
            .Key("tenant", x => x.Tenant).Key("region", x => x.Region).Key("id", x => x.Id)
            .Field("blob", "blob", x => x.Blob, x => x.Blob)
            .Collection("children", "children", x => x.Children, x => x.Children, Child(), Query());
        var result = await new QueryableExpressionToLinqTransformer().RenderProjection(ProjectionDirective.All, map)
            .ExecuteAsync(data.AsQueryable(), new MemoryProjectionExecutor(), options: new ProjectionExecutionOptions { MaxKeyParameters = 3 });
        Assert.Equal("beta", result.Items[0].Children[0].Name);
        Assert.Equal("gamma", result.Items[1].Children[0].Name);
        Assert.Equal(new byte[] { 1, 2 }, ProjectionShaper.ToSparseObjects(result)[0]["blob"]);
        Assert.Equal(firstGuid, result.GetKey(0)[0]);
    }

    [Fact]
    public async Task CollectionDefaultsAndDescendingOrderingAreApplied()
    {
        var defaultSort = new SortDirective(new[] {
            new SortDirectiveItem { Expression = new Field("name", typeof(string)), Direction = SortDirection.Descending },
            new SortDirectiveItem { Expression = new Field("name", typeof(string)), Direction = SortDirection.Ascending } });
        var map = Parent().Collection("children", "children", x => x.Children, x => x.Children, Child(), Query(), defaultSort);
        var plan = new QueryableExpressionToLinqTransformer().RenderProjection(ProjectionDirective.All, map);
        await Assert.ThrowsAsync<ArgumentException>(() => plan.ExecuteAsync(ProjectionFixture.Data().AsQueryable(), new MemoryProjectionExecutor()));
        var result = await plan.ExecuteAsync(ProjectionFixture.Data().Take(1).AsQueryable(), new MemoryProjectionExecutor(), new QueryableExpressionToLinqTransformer());
        Assert.Equal("beta", result.Items[0].Children[0].Name);
        Assert.Equal("alpha", result.Items[0].Children[1].Name);
    }

    [Fact]
    public async Task SharedChildrenAndDuplicateRelationshipsAttachOncePerParent()
    {
        var data = ProjectionFixture.Data();
        data[0].Children.Add(data[0].Children[0]);
        data.Add(data[0]);
        var result = await new QueryableExpressionToLinqTransformer().RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping())
            .ExecuteAsync(data.AsQueryable(), new MemoryProjectionExecutor());
        Assert.Equal(2, result.Items[0].Children.Count);
        Assert.Equal(2, result.Items[3].Children.Count);
        Assert.Single(result.Items[0].Children[0].Leaves);
        Assert.Single(result.Items[3].Children[0].Leaves);
    }

    [Fact]
    public async Task NullableKeyComponentsDoNotAttachIncompleteIdentities()
    {
        var child = Child();
        var map = new LinqProjectionMapping<NullableParent, ProjectionParentDto>().Key("tenant", x => x.Tenant).Key("id", x => x.Id)
            .Collection("children", "children", x => x.Children, x => x.Children, child, Query());
        var data = new[] { new NullableParent { Tenant = null, Id = 1, Children = ProjectionFixture.Data()[0].Children } };
        var executor = new MemoryProjectionExecutor();
        var result = await new QueryableExpressionToLinqTransformer().RenderProjection(ProjectionDirective.All, map).ExecuteAsync(data.AsQueryable(), executor);
        Assert.Empty(result.Items[0].Children);
        Assert.Single(executor.Queries);
    }

    public sealed class InvalidDto
    {
        public List<ProjectionChild> Entities { get; set; } = new List<ProjectionChild>();
        public int ValueField;
        public readonly int ReadOnlyField;
        public int GetOnly { get; }
        public int PrivateSetter { get; private set; }
        public int WriteOnly { private get; set; }
        public ICollection<ProjectionChildDto> NullChildren { get; } = null!;
        public ICollection<ProjectionChildDto> ReadOnlyChildren { get; } = Array.Empty<ProjectionChildDto>();
    }
    public sealed class NullableParent
    {
        public int? Tenant { get; set; }
        public int Id { get; set; }
        public List<ProjectionChild> Children { get; set; } = new List<ProjectionChild>();
    }
    public sealed class TreeEntity
    {
        public int Id { get; set; }
        public List<TreeEntity> Children { get; set; } = new List<TreeEntity>();
    }
    public sealed class TreeDto
    {
        public List<TreeDto> Children { get; set; } = new List<TreeDto>();
        public List<TreeDto> OtherChildren { get; set; } = new List<TreeDto>();
    }
    public sealed class HeterogeneousParent
    {
        public Guid Tenant { get; set; }
        public string Region { get; set; } = "";
        public int Id { get; set; }
        public byte[] Blob { get; set; } = Array.Empty<byte>();
        public List<ProjectionChild> Children { get; set; } = new List<ProjectionChild>();
    }
    public sealed class HeterogeneousDto
    {
        public byte[] Blob { get; set; } = Array.Empty<byte>();
        public List<ProjectionChildDto> Children { get; set; } = new List<ProjectionChildDto>();
    }
}
