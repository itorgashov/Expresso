using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.EntityFramework.Test
{
    /// <summary>Every shared catalog and differential case translates on the EF6 SQL Server provider.</summary>
    public class Ef6TranslationTests
    {
        [Theory]
        [MemberData(nameof(RendererIntegrationCases.FilterCases), MemberType = typeof(RendererIntegrationCases))]
        public void Filter_Translates(FilterCase c)
        {
            using var context = new TestWidgetEf6Context();
            Assert.Contains("WHERE", context.WhereSql(c.Filter));
        }

        [Theory]
        [MemberData(nameof(RendererIntegrationCases.ParentSortCases), MemberType = typeof(RendererIntegrationCases))]
        public void ParentSort_Translates(ParentSortCase c)
        {
            using var context = new TestWidgetEf6Context();
            Assert.Contains("ORDER BY", context.OrderSql(c.Sort, c.Filter));
        }

        [Theory]
        [MemberData(nameof(RendererDifferentialCases.Cases), MemberType = typeof(RendererDifferentialCases))]
        public void Differential_Translates(DifferentialCase c)
        {
            using var context = new TestWidgetEf6Context();
            var sql = c.Sort is null ? context.WhereSql(c.Filter!) : context.OrderSql(c.Sort, c.Filter);
            Assert.False(string.IsNullOrEmpty(sql));
        }
    }
}
