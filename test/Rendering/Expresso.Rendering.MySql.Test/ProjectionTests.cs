using Expresso.Rendering.TestCases;
using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering.MySql.Test;

public sealed class ProjectionTests : ProjectionSqlTests
{
    public ProjectionTests() : base(new ExpressionToMySqlQueryClauseTransformer(), "`", "`") { }

    [Fact]
    public void ProjectionRegistration_UsesSameRendererInstance()
    {
        using var services = new ServiceCollection().AddMySqlExpressionTransformations().BuildServiceProvider();
        Assert.Same(services.GetRequiredService<IExpressionToQueryClauseTransformer>(),
            services.GetRequiredService<IProjectionToSqlTransformer>());
    }
}

