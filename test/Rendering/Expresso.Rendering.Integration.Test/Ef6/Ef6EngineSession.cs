#if NETFRAMEWORK
using System.Data.Common;
using System.Data.Entity.Core;
using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Sorting;
using Expresso.Rendering.EntityFramework;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.Integration.Test.Ef6
{
    /// <summary>Runs the shared catalog through EF6 on its own provider connection to an ADO fixture's seeded database.</summary>
    public sealed class Ef6EngineSession : IEngineSession
    {
        private readonly Func<DbConnection> _connect;
        private readonly string? _schema;
        private readonly LinqQueryMapping<Widget> _mapping = WidgetLinqMapping.Create();
        private readonly LinqQueryMapping<WidgetTag> _tagMapping = WidgetLinqMapping.Tags();
        private readonly Ef6Provider _provider;

        public Ef6EngineSession(Func<DbConnection> connect, string? schema = null, Ef6Provider provider = Ef6Provider.Other)
        {
            _connect = connect;
            _schema = schema;
            _provider = provider;
        }

        public IReadOnlyList<int> QueryWidgetIds(FilterCriteria? filter, SortDirective? sort, PagingDirective? paging = null)
        {
            using var context = NewContext();
            return Run(WidgetIds(context, filter, sort, paging));
        }

        /// <summary>SQL EF6 generates for <see cref="QueryWidgetIds"/> (diagnostics), or the translation error.</summary>
        public string Sql(FilterCriteria? filter, SortDirective? sort)
        {
            using var context = NewContext();
            try
            {
                return WidgetIds(context, filter, sort, paging: null).ToString();
            }
            catch (NotSupportedException ex)
            {
                return ex.Message;
            }
        }

        public IReadOnlyList<string> QueryTagLabels(int widgetId, SortDirective sort)
        {
            using var context = NewContext();
            var transformer = new Ef6ExpressionToLinqTransformer(context);
            return Run(context.Tags
                .Where(t => t.WidgetId == widgetId)
                .OrderBy(transformer, sort, _tagMapping)
                .Select(t => t.Label));
        }

        private WidgetEf6Context NewContext() => new(_connect(), _schema);

        private IQueryable<int> WidgetIds(WidgetEf6Context context, FilterCriteria? filter, SortDirective? sort, PagingDirective? paging)
        {
            var transformer = new Ef6ExpressionToLinqTransformer(context);
            IQueryable<Widget> query = context.Widgets;
            if (filter is not null)
            {
                query = query.Where(transformer, filter, _mapping);
            }

            var ordered = sort is null ? query.OrderBy(w => w.Id) : query.OrderBy(transformer, sort, _mapping);
            var windowed = paging is null || paging.IsEmpty ? ordered : ordered.Page(paging, _provider);
            return windowed.Select(w => w.Id);
        }

        private static List<TResult> Run<TResult>(IQueryable<TResult> query)
        {
            try
            {
                return query.ToList();
            }
            catch (EntityCommandExecutionException ex)
            {
                throw new InvalidOperationException((ex.InnerException ?? ex).Message + Environment.NewLine + query, ex);
            }
        }
    }
}
#endif
