using Expresso.Rendering.TestCases;
using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering.PostgreSql.Test;

public sealed class ProjectionTests : ProjectionSqlTests
{
    public ProjectionTests() : base(new ExpressionToPostgreSqlQueryClauseTransformer(), "\"", "\"") { }

    [Fact]
    public void ProjectionRegistration_UsesSameRendererInstance()
    {
        using var services = new ServiceCollection().AddPostgreSqlExpressionTransformations().BuildServiceProvider();
        Assert.Same(services.GetRequiredService<IExpressionToQueryClauseTransformer>(),
            services.GetRequiredService<IProjectionToSqlTransformer>());
    }
}

