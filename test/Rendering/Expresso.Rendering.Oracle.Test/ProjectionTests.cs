using Expresso.Rendering.TestCases;
using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering.Oracle.Test;

public sealed class ProjectionTests : ProjectionSqlTests
{
    public ProjectionTests() : base(new ExpressionToOracleQueryClauseTransformer(), "\"", "\"") { }

    [Fact]
    public void ProjectionRegistration_UsesSameRendererInstance()
    {
        using var services = new ServiceCollection().AddOracleExpressionTransformations().BuildServiceProvider();
        Assert.Same(services.GetRequiredService<IExpressionToQueryClauseTransformer>(),
            services.GetRequiredService<IProjectionToSqlTransformer>());
    }
}

