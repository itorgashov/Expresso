using System.Data.Entity;
using System.Reflection;
using Expresso.Rendering.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering.EntityFramework.Test
{
    public class Ef6FunctionsAndRegistrationTests
    {
        public static IEnumerable<object[]> Stubs() =>
            typeof(Ef6Functions).GetMethods(BindingFlags.Public | BindingFlags.Static).Select(m => new object[] { m.Name });

        [Theory]
        [MemberData(nameof(Stubs))]
        public void Stub_IsMappedAndThrowsInMemory(string name)
        {
            var method = typeof(Ef6Functions).GetMethod(name)!;
            Assert.NotNull(method.GetCustomAttribute<DbFunctionAttribute>());
            var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object?[method.GetParameters().Length]));
            Assert.IsType<NotSupportedException>(error.InnerException);
        }

        [Fact]
        public void AddEf6ExpressionTransformations_RegistersOneTransformer()
        {
            using var provider = new ServiceCollection().AddEf6ExpressionTransformations("Npgsql").BuildServiceProvider();
            var transformer = provider.GetRequiredService<Ef6ExpressionToLinqTransformer>();
            Assert.Equal(Ef6Provider.PostgreSql, transformer.Provider);
            Assert.Same(transformer, provider.GetRequiredService<IExpressionToLinqTransformer>());
        }
    }
}
