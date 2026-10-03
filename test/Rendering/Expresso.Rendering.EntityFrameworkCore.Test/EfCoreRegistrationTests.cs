using Expresso.Rendering.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering.EntityFrameworkCore.Test
{
    /// <summary>Model registration of the markers and DI registration of the transformer.</summary>
    public class EfCoreRegistrationTests
    {
        [Fact]
        public void HasExpressoFunctions_RegistersTheProviderTranslations()
        {
            using var context = TestWidgetContext.SqlServer();

            var functions = context.Model.GetDbFunctions().Select(f => f.MethodInfo).ToList();

            Assert.Equal(ExpressoFunctionTranslations.For(EfCoreProvider.SqlServer).Select(e => e.Key).OrderBy(m => m.ToString()), functions.OrderBy(m => m!.ToString()));
        }

        [Fact]
        public void HasExpressoFunctions_UnknownProvider_RegistersNothing()
        {
            var modelBuilder = new ModelBuilder();

            modelBuilder.HasExpressoFunctions("Some.Other.Provider");

            Assert.Empty(modelBuilder.Model.GetDbFunctions());
        }

        [Fact]
        public void HasExpressoFunctions_NullBuilder_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ((ModelBuilder)null!).HasExpressoFunctions("Microsoft.EntityFrameworkCore.Sqlite"));
        }

        [Fact]
        public void AddEfCoreExpressionTransformations_ResolvesScopedTransformerForTheContextProvider()
        {
            var services = new ServiceCollection()
                .AddDbContext<TestWidgetContext>(o => o.UseSqlite("Data Source=unused.db"))
                .AddEfCoreExpressionTransformations<TestWidgetContext>();
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            var transformer = scope.ServiceProvider.GetRequiredService<IExpressionToLinqTransformer>();

            var concrete = Assert.IsType<EfCoreExpressionToLinqTransformer>(transformer);
            Assert.Equal(EfCoreProvider.Sqlite, concrete.Provider);
            Assert.Same(concrete, scope.ServiceProvider.GetRequiredService<EfCoreExpressionToLinqTransformer>());
        }
    }
}
