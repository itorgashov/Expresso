using System.Text.RegularExpressions;
using Expresso.Core.Filtering;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.EntityFramework.Test
{
    /// <summary>Shared catalog and differential filters by case id.</summary>
    internal static class Ef6Cases
    {
        public static FilterCriteria Filter(string id) =>
            RendererIntegrationCases.AllFilters().SingleOrDefault(c => c.Id == id)?.Filter
            ?? RendererDifferentialCases.Cases().Select(o => (DifferentialCase)o[0]).Single(c => c.Id == id).Filter!;

        /// <summary>Collapses whitespace runs, so fragments don't depend on EF6's line layout.</summary>
        public static string Normalize(string sql) => Regex.Replace(sql, @"\s+", " ");
    }
}
