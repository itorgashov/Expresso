#if NET8_0_OR_GREATER
using System.Data.Common;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.Integration.Test.Ef
{
    /// <summary>Runs the shared catalog through EF Core on the connection (and seed) of an ADO fixture.</summary>
    public sealed class EfEngineSession : IEngineSession
    {
        private readonly DbContextOptions<WidgetDbContext> _options;
        private readonly LinqQueryMapping<Widget> _mapping = WidgetLinqMapping.Create();
        private readonly LinqQueryMapping<WidgetTag> _tagMapping = WidgetLinqMapping.Tags();
        private readonly bool _liftSortKeys;

        public EfEngineSession(
            DbConnection connection,
            Action<DbContextOptionsBuilder<WidgetDbContext>, DbConnection> configure,
            bool liftSortKeys = false)
        {
            var builder = new DbContextOptionsBuilder<WidgetDbContext>();
            configure(builder, connection);
            _options = builder.Options;
            _liftSortKeys = liftSortKeys;
        }

        public IReadOnlyList<int> QueryWidgetIds(FilterCriteria? filter, SortDirective? sort)
        {
            using var context = new WidgetDbContext(_options);
            return Run(WidgetIds(context, filter, sort));
        }

        /// <summary>SQL EF generates for <see cref="QueryWidgetIds"/> (diagnostics), or the translation error.</summary>
        public string Sql(FilterCriteria? filter, SortDirective? sort)
        {
            using var context = new WidgetDbContext(_options);
            try
            {
                return WidgetIds(context, filter, sort).ToQueryString();
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }
        }

        private IQueryable<int> WidgetIds(WidgetDbContext context, FilterCriteria? filter, SortDirective? sort)
        {
            var transformer = new EfCoreExpressionToLinqTransformer(context.Database.ProviderName);
            IQueryable<Widget> query = context.Widgets;
            if (filter is not null)
            {
                query = query.Where(transformer, filter, _mapping);
            }

            return sort is not null && _liftSortKeys
                ? query.OrderedKeys(transformer, sort, _mapping, w => w.Id)
                : (sort is null ? query.OrderBy(w => w.Id) : query.OrderBy(transformer, sort, _mapping)).Select(w => w.Id);
        }

        private static List<TResult> Run<TResult>(IQueryable<TResult> query)
        {
            try
            {
                return query.ToList();
            }
            catch (DbException ex)
            {
                throw new InvalidOperationException(ex.Message + Environment.NewLine + query.ToQueryString(), ex);
            }
        }

        /// <summary>Tag labels of <paramref name="widgetId"/> loaded through <c>IncludeSorted</c> with <c>sortfor tags</c> = <paramref name="tagSort"/>.</summary>
        public IReadOnlyList<string> QueryIncludedTagLabels(int widgetId, SortDirective tagSort)
        {
            using var context = new WidgetDbContext(_options);
            var transformer = new EfCoreExpressionToLinqTransformer(context.Database.ProviderName);
            var root = new SortDirective(Array.Empty<SortDirectiveItem>(), new[] { new CollectionSort("tags", tagSort) });
            var widget = context.Widgets.Where(w => w.Id == widgetId).IncludeSorted(transformer, root, _mapping).Single();
            return widget.Tags.Select(t => t.Label).ToList();
        }

        public IReadOnlyList<string> QueryTagLabels(int widgetId, SortDirective sort)
        {
            using var context = new WidgetDbContext(_options);
            var transformer = new EfCoreExpressionToLinqTransformer(context.Database.ProviderName);
            return Run(context.Tags
                .Where(t => t.WidgetId == widgetId)
                .OrderBy(transformer, sort, _tagMapping)
                .Select(t => t.Label));
        }
    }
}
#endif
