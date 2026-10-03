using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.Linq.Test
{
    /// <summary>
    /// The shared integration catalog (same cases and expected ids as the ADO engine tests) run against the seed
    /// object graph. In-memory uses <see cref="InMemoryExpressionToLinqTransformer"/> over <c>List&lt;Widget&gt;</c>;
    /// Queryable runs the default lambdas through <c>AsQueryable()</c>, which checks they are valid and correct on ASCII seed data.
    /// </summary>
    public class CatalogTests
    {
        private static readonly LinqQueryMapping<Widget> Mapping = WidgetLinqMapping.Create();
        private static readonly InMemoryExpressionToLinqTransformer InMemoryTransformer = new();
        private static readonly QueryableExpressionToLinqTransformer QueryableTransformer = new();

        [Theory]
        [MemberData(nameof(RendererIntegrationCases.FilterCases), MemberType = typeof(RendererIntegrationCases))]
        public void InMemory_Filter_ReturnsExpectedIds(FilterCase testCase)
        {
            var ids = WidgetSeedData.CreateWidgets()
                .Where(InMemoryTransformer, testCase.Filter, Mapping)
                .Select(w => w.Id)
                .OrderBy(id => id);
            Assert.Equal(testCase.ExpectedIdsOrdered, ids);
        }

        [Theory]
        [MemberData(nameof(RendererIntegrationCases.FilterCases), MemberType = typeof(RendererIntegrationCases))]
        public void Queryable_Filter_ReturnsExpectedIds(FilterCase testCase)
        {
            var ids = WidgetSeedData.CreateWidgets().AsQueryable()
                .Where(QueryableTransformer, testCase.Filter, Mapping)
                .Select(w => w.Id)
                .OrderBy(id => id);
            Assert.Equal(testCase.ExpectedIdsOrdered, ids);
        }

        [Theory]
        [MemberData(nameof(RendererIntegrationCases.ParentSortCases), MemberType = typeof(RendererIntegrationCases))]
        public void InMemory_ParentSort_ReturnsExpectedIds(ParentSortCase testCase)
        {
            IEnumerable<Widget> widgets = WidgetSeedData.CreateWidgets();
            if (testCase.Filter is not null)
            {
                widgets = widgets.Where(InMemoryTransformer, testCase.Filter, Mapping);
            }

            var ids = widgets.OrderBy(InMemoryTransformer, testCase.Sort, Mapping).Select(w => w.Id);
            Assert.Equal(testCase.ExpectedIdsOrdered, ids);
        }

        [Theory]
        [MemberData(nameof(RendererIntegrationCases.ParentSortCases), MemberType = typeof(RendererIntegrationCases))]
        public void Queryable_ParentSort_ReturnsExpectedIds(ParentSortCase testCase)
        {
            var widgets = WidgetSeedData.CreateWidgets().AsQueryable();
            if (testCase.Filter is not null)
            {
                widgets = widgets.Where(QueryableTransformer, testCase.Filter, Mapping);
            }

            var ids = widgets.OrderBy(QueryableTransformer, testCase.Sort, Mapping).Select(w => w.Id);
            Assert.Equal(testCase.ExpectedIdsOrdered, ids);
        }

        [Theory]
        [MemberData(nameof(RendererIntegrationCases.NestedSortCases), MemberType = typeof(RendererIntegrationCases))]
        public void InMemory_NestedSort_ReturnsExpectedLabels(NestedSortCase testCase)
        {
            var widget = WidgetSeedData.CreateWidgets().Single(w => w.Id == testCase.WidgetId);
            var labels = widget.Tags.OrderBy(InMemoryTransformer, testCase.Sort, WidgetLinqMapping.Tags()).Select(t => t.Label);
            Assert.Equal(testCase.ExpectedLabels, labels);
        }

        [Theory]
        [MemberData(nameof(RendererIntegrationCases.NestedSortCases), MemberType = typeof(RendererIntegrationCases))]
        public void Queryable_NestedSort_ReturnsExpectedLabels(NestedSortCase testCase)
        {
            var widget = WidgetSeedData.CreateWidgets().Single(w => w.Id == testCase.WidgetId);
            var labels = widget.Tags.AsQueryable().OrderBy(QueryableTransformer, testCase.Sort, WidgetLinqMapping.Tags()).Select(t => t.Label);
            Assert.Equal(testCase.ExpectedLabels, labels);
        }
    }
}
