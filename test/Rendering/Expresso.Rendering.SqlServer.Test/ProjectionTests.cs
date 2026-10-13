using Expresso.Rendering.TestCases;
using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering.SqlServer.Test;

public sealed class ProjectionTests : ProjectionSqlTests
{
    public ProjectionTests() : base(new ExpressionToSqlServerQueryClauseTransformer(), "[", "]") { }

    [Fact]
    public void ProjectionRegistration_UsesSameRendererInstance()
    {
        using var services = new ServiceCollection().AddSqlServerExpressionTransformations().BuildServiceProvider();
        Assert.Same(services.GetRequiredService<IExpressionToQueryClauseTransformer>(),
            services.GetRequiredService<IProjectionToSqlTransformer>());
    }
}

