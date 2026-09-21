using Microsoft.Extensions.Configuration;

namespace Expresso.Rendering.Integration.Test
{
    public static class IntegrationEnabled
    {
        public const string SkipReason =
            "Set EXPRESSO_IT=1 (or IntegrationTests:Enabled=true) to run integration tests.";

        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets(typeof(IntegrationEnabled).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        public static bool IsOn { get; } = Compute();

        public static string? ConnectionString(string name) =>
            Configuration[$"IntegrationTests:ConnectionStrings:{name}"];

        private static bool Compute()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("EXPRESSO_IT"), "1", StringComparison.Ordinal))
            {
                return true;
            }

            return string.Equals(Configuration["IntegrationTests:Enabled"], "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
