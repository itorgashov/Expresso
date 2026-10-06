using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.Integration.Test
{
    /// <summary>LINQ-to-objects over the seed graph with the in-memory profile (PostgreSQL reference semantics).</summary>
    public sealed class InMemoryEngineSession : IEngineSession
    {
        private readonly List<Widget> _widgets = WidgetSeedData.CreateWidgets();
        private readonly InMemoryExpressionToLinqTransformer _transformer = new();
        private readonly LinqQueryMapping<Widget> _mapping = WidgetLinqMapping.Create();
        private readonly LinqQueryMapping<WidgetTag> _tagMapping = WidgetLinqMapping.Tags();

        public IReadOnlyList<int> QueryWidgetIds(FilterCriteria? filter, SortDirective? sort)
        {
            IEnumerable<Widget> rows = _widgets;
            if (filter is not null)
            {
                rows = rows.Where(_transformer, filter, _mapping);
            }

            var ordered = sort is null ? rows.OrderBy(w => w.Id) : rows.OrderBy(_transformer, sort, _mapping);
            return ordered.Select(w => w.Id).ToList();
        }

        public IReadOnlyList<string> QueryTagLabels(int widgetId, SortDirective sort) =>
            _widgets.Single(w => w.Id == widgetId).Tags.OrderBy(_transformer, sort, _tagMapping).Select(t => t.Label).ToList();
    }

    // In-memory results must equal the PostgreSQL ADO results (the in-memory reference engine) on every case.
    [Trait("Category", "Integration")]
    [Collection("PostgreSqlIT")]
    public sealed class InMemoryVsPostgreSqlItTests
    {
        private readonly IEngineSession _postgres;
        private readonly InMemoryEngineSession _inMemory = new();

        public InMemoryVsPostgreSqlItTests(PostgreSqlItFixture fixture)
        {
            _postgres = fixture.Session!;
        }

        [SkippableTheory]
        [MemberData(nameof(RendererDifferentialCases.CollationFreeCases), MemberType = typeof(RendererDifferentialCases))]
        public void Differential_InMemoryMatchesPostgreSql(DifferentialCase testCase)
        {
            Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
            AssertSame(testCase.Filter, testCase.Sort, testCase.Rejection, testCase.UnsupportedReason, testCase.DatabaseCodes);
        }

        [SkippableTheory]
        [MemberData(nameof(RendererIntegrationCases.FilterCases), MemberType = typeof(RendererIntegrationCases))]
        public void Filter_InMemoryMatchesPostgreSql(FilterCase testCase)
        {
            Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
            AssertSame(testCase.Filter, null);
        }

        [SkippableTheory]
        [MemberData(nameof(RendererIntegrationCases.ParentSortCases), MemberType = typeof(RendererIntegrationCases))]
        public void ParentSort_InMemoryMatchesPostgreSql(ParentSortCase testCase)
        {
            Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
            AssertSame(testCase.Filter, testCase.Sort);
        }

        private void AssertSame(
            FilterCriteria? filter,
            SortDirective? sort,
            DifferentialRejection rejection = DifferentialRejection.None,
            string? unsupportedReason = null,
            IReadOnlyList<string>? databaseCodes = null) =>
            DifferentialOutcome.AssertSame(
                DifferentialOutcome.Of(() => _postgres.QueryWidgetIds(filter, sort)),
                DifferentialOutcome.Of(() => _inMemory.QueryWidgetIds(filter, sort)),
                rejection,
                unsupportedReason: unsupportedReason,
                databaseCodes: databaseCodes);
    }
}
