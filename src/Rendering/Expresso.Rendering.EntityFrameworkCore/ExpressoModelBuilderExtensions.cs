using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore
{
    /// <summary>Registers the <see cref="ExpressoDbFunctions"/> translations for a provider.</summary>
    public static class ExpressoModelBuilderExtensions
    {
        /// <summary>
        /// Call from <c>OnModelCreating</c> with <c>Database.ProviderName</c>. Registers the marker translations used by
        /// <see cref="EfCoreExpressionToLinqTransformer"/> for the same provider.
        /// </summary>
        /// <param name="modelBuilder">Model builder.</param>
        /// <param name="providerName"><c>DbContext.Database.ProviderName</c>.</param>
        /// <returns>The same <paramref name="modelBuilder"/>.</returns>
        public static ModelBuilder HasExpressoFunctions(this ModelBuilder modelBuilder, string? providerName)
        {
            if (modelBuilder is null)
            {
                throw new ArgumentNullException(nameof(modelBuilder));
            }

            foreach (var (method, translation) in ExpressoFunctionTranslations.For(EfCoreProviders.Resolve(providerName)))
            {
                modelBuilder.HasDbFunction(method).HasTranslation(translation);
            }

            return modelBuilder;
        }
    }
}
