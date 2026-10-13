using System.Linq.Expressions;
using Expresso.Core.Projection;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.Linq.Test;

public sealed class ProjectionRemediationTests
{
    [Fact]
    public async Task SingleScalarKeys_UseContains_PreserveDistinctNullChildren_AndDeduplicateCompleteKeys()
    {
        // Scenario: two children have incomplete identities and two share a complete identity; only the complete duplicate collapses.
        var children = new LinqProjectionMapping<Child, ChildDto>().Key("id", c => c.Id)
            .Field("name", "name", c => c.Name, c => c.Name);
        var mapping = new LinqProjectionMapping<Parent, ParentDto>().Key("id", p => p.Id)
            .Collection("children", "children", p => p.Children, p => p.Children, children, new LinqQueryMapping<Child>());
        var source = new[] { new Parent { Id = 1, Children = new List<Child> {
            new Child { Name = "null-one" }, new Child { Name = "null-two" },
            new Child { Id = 1, Name = "complete" }, new Child { Id = 1, Name = "duplicate" } } } }.AsQueryable();
        var executor = new MemoryProjectionExecutor();
        var result = await new QueryableExpressionToLinqTransformer().RenderProjection(
            new ProjectionDirective(new[] { new ProjectionSelection("children") }), mapping).ExecuteAsync(source, executor);
        Assert.Equal(new[] { "null-one", "null-two", "complete" }, result.Items[0].Children.Select(c => c.Name));
        Assert.Contains("Contains", executor.Expressions[1].ToString());
    }

    [Fact]
    public async Task DefaultKeySource_RemovesOnlyOuterWindows_RetainsAuthorization_AndHandlesNestedWhere()
    {
        // Scenario: authorized tenant filters surround the request page; a later insert must not shift child lookup or expose another tenant.
        var data = ProjectionFixture.Data();
        var source = data.AsQueryable().Where(p => p.Tenant == 1).OrderBy(p => p.Title).Skip(1).Take(1).Where(p => p.Id == 1);
        var executor = new ShiftExecutor(() => data.Add(new ProjectionParent { Tenant = 1, Id = 3, Title = "aaa" }));
        var result = await new QueryableExpressionToLinqTransformer().RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping())
            .ExecuteAsync(source, executor);
        Assert.Equal("one", Assert.Single(result.Items).Title);
        Assert.Equal("first", result.Items[0].Children.Single(c => c.Name == "beta").Leaves.Single().Value);
    }

    public sealed class Parent
    {
        public int Id { get; set; }
        public List<Child> Children { get; set; } = new();
    }
    public sealed class Child
    {
        public int? Id { get; set; }
        public string Name { get; set; } = "";
    }
    public sealed class ParentDto { public List<ChildDto> Children { get; set; } = new(); }
    public sealed class ChildDto { public string Name { get; set; } = ""; }
    private sealed class ShiftExecutor : IProjectionQueryExecutor
    {
        private readonly Action _shift;
        private int _reads;
        internal ShiftExecutor(Action shift) => _shift = shift;
        public Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            var rows = query.ToList();
            if (_reads++ == 0) _shift();
            return Task.FromResult<IReadOnlyList<T>>(rows);
        }
    }
}
