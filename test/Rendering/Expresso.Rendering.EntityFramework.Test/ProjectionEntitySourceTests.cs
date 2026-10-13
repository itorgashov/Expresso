using System.Reflection;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.EntityFramework.Test;

public sealed class ProjectionEntitySourceTests
{
    [Fact]
    public void Resolver_UsesContextEntitySet_AndRejectsUnsupportedSources()
    {
        using var db = new ProjectionContext();
        var scope = db.Parents.Where(x => x.Tenant == 1).SelectMany(x => x.Children);
        var source = Ef6ProjectionEntitySource.Resolve(scope, typeof(ProjectionChild));
        Assert.IsAssignableFrom<IQueryable<ProjectionChild>>(source);
        Assert.Contains("widget_projection_child", source!.ToString());
        Assert.DoesNotContain("widget_projection_parent", source.ToString());
        Assert.Null(Ef6ProjectionEntitySource.Resolve(scope, typeof(int)));
        Assert.Null(Ef6ProjectionEntitySource.Resolve(ProjectionFixture.Data().AsQueryable(), typeof(ProjectionParent)));
        var error = Assert.Throws<InvalidOperationException>(() => Ef6ProjectionEntitySource.Resolve(scope, typeof(ProjectionParentDto)));
        Assert.IsNotType<TargetInvocationException>(error);
    }

    [Fact]
    public void AdapterExecutor_ResolvesRoots_EvenWithoutProjectionTransformerCapability()
    {
        using var db = new ProjectionContext();
        var executor = (IProjectionEntitySourceProvider)Activator.CreateInstance(typeof(Ef6ProjectionExtensions)
            .GetNestedType("Executor", BindingFlags.NonPublic)!, nonPublic: true)!;
        Assert.IsAssignableFrom<IQueryable<ProjectionChild>>(executor.GetEntitySource(db.Parents, typeof(ProjectionChild)));
    }
}
