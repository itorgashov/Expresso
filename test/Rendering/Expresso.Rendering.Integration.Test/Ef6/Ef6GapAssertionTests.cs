#if NETFRAMEWORK
using System.Data.Common;
using Expresso.Rendering.EntityFramework;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.Integration.Test.Ef6
{
    public sealed class Ef6GapAssertionTests
    {
        [Fact]
        public void IsNullSqrtNeg_IsADocumentedProviderGap()
        {
            var filter = RendererDifferentialCases.All().Single(c => c.Id == "isnull-sqrt-neg").Filter!;
            AssertGap(Ef6Provider.PostgreSql, Ef6ProviderGaps.PostgreSql, filter);
            AssertGap(Ef6Provider.Oracle, Ef6ProviderGaps.Oracle, filter);
        }

        [Fact]
        public void DatabaseGap_RequiresTheOraNumber()
        {
            var gap = new Ef6Gap("EF6's concat null guard emits N'' (ORA-12704 on VARCHAR2 columns)", Kind: Ef6GapKind.Database);

            Ef6GapAssertions.Assert(gap, new NumberedDbException(12704));
            Assert.NotNull(Record.Exception(() => Ef6GapAssertions.Assert(gap, new NumberedDbException(942))));
        }

        private static void AssertGap(Ef6Provider provider, IReadOnlyDictionary<string, Ef6Gap> gaps, Expresso.Core.Filtering.FilterCriteria filter)
        {
            Assert.True(gaps.TryGetValue("isnull-sqrt-neg", out var gap));
            var ex = Assert.Throws<NotSupportedException>(() =>
                new Ef6ExpressionToLinqTransformer(provider).BuildPredicate(filter, WidgetLinqMapping.Create()));
            Ef6GapAssertions.Assert(gap!, ex);
        }

        private sealed class NumberedDbException : DbException
        {
            public NumberedDbException(int number) => Number = number;

            public int Number { get; }
        }
    }
}
#endif
