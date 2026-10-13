using Expresso.Rendering.TestCases;
using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering.Sqlite.Test;

public sealed class ProjectionTests : ProjectionSqlTests
{
    public ProjectionTests() : base(new ExpressionToSqliteQueryClauseTransformer(), "\"", "\"") { }

    [Fact]
    public void ProjectionRegistration_UsesSameRendererInstance()
    {
        using var services = new ServiceCollection().AddSqliteExpressionTransformations().BuildServiceProvider();
        Assert.Same(services.GetRequiredService<IExpressionToQueryClauseTransformer>(),
            services.GetRequiredService<IProjectionToSqlTransformer>());
    }
}

