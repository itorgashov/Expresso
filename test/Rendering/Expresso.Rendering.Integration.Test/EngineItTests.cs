using System.Data.Common;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Rendering;

namespace Expresso.Rendering.Integration.Test
{
    [Trait("Category", "Integration")]
    public abstract class EngineItTests
    {
        protected abstract IEngineSession Session { get; }

        [SkippableTheory]
        [MemberData(nameof(RendererIntegrationCases.FilterCases), MemberType = typeof(RendererIntegrationCases))]
        public void Filter_ReturnsExpectedIds(FilterCase testCase)
        {
            Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
            var ids = Session.QueryWidgetIds(testCase.Filter, sort: null);
            Assert.Equal(testCase.ExpectedIdsOrdered, ids);
        }

        [SkippableTheory]
        [MemberData(nameof(RendererIntegrationCases.ParentSortCases), MemberType = typeof(RendererIntegrationCases))]
        public void ParentSort_ReturnsExpectedIds(ParentSortCase testCase)
        {
            Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
            var ids = Session.QueryWidgetIds(testCase.Filter, testCase.Sort);
            Assert.Equal(testCase.ExpectedIdsOrdered, ids);
        }

        [SkippableTheory]
        [MemberData(nameof(RendererIntegrationCases.NestedSortCases), MemberType = typeof(RendererIntegrationCases))]
        public void NestedSort_ReturnsExpectedLabels(NestedSortCase testCase)
        {
            Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
            var labels = Session.QueryTagLabels(testCase.WidgetId, testCase.Sort);
            Assert.Equal(testCase.ExpectedLabels, labels);
        }
    }

    public interface IEngineSession
    {
        IReadOnlyList<int> QueryWidgetIds(FilterCriteria? filter, SortDirective? sort);

        IReadOnlyList<string> QueryTagLabels(int widgetId, SortDirective sort);
    }

    public sealed class EngineSession : IEngineSession
    {
        private readonly DbConnection _connection;
        private readonly IExpressionToQueryClauseTransformer _transformer;
        private readonly SqlQueryMapping _mapping;
        private readonly SqlQueryMapping _tagMapping;
        private readonly Action<DbCommand, string, object> _bind;
        private readonly string _widgetTable;
        private readonly string _tagTable;
        private readonly string _idColumn;
        private readonly string _labelColumn;
        private readonly string _widgetIdColumn;
        private readonly bool _orderByInSelectList;

        public EngineSession(
            DbConnection connection,
            IExpressionToQueryClauseTransformer transformer,
            SqlQueryMapping mapping,
            SqlQueryMapping tagMapping,
            Action<DbCommand, string, object> bind,
            string widgetTable = "widget",
            string tagTable = "widget_tag",
            string idColumn = "id",
            string labelColumn = "label",
            string widgetIdColumn = "widget_id",
            bool orderByInSelectList = false)
        {
            _connection = connection;
            _transformer = transformer;
            _mapping = mapping;
            _tagMapping = tagMapping;
            _bind = bind;
            _widgetTable = widgetTable;
            _tagTable = tagTable;
            _idColumn = idColumn;
            _labelColumn = labelColumn;
            _widgetIdColumn = widgetIdColumn;
            _orderByInSelectList = orderByInSelectList;
        }

        public IReadOnlyList<int> QueryWidgetIds(FilterCriteria? filter, SortDirective? sort)
        {
            var parameters = new Dictionary<string, object>();
            var whereSql = string.Empty;
            if (filter is not null)
            {
                var rendered = _transformer.RenderWhereClause(filter, _mapping, WidgetMapping.ParamPrefix);
                whereSql = " WHERE " + rendered.whereClause;
                Merge(parameters, rendered.parameters);
            }

            string sql;
            if (sort is not null && _orderByInSelectList)
            {
                sql = BuildLiftedSortSql(sort, whereSql, parameters);
            }
            else
            {
                sql = $"SELECT {_idColumn} FROM {_widgetTable}{whereSql}";
                if (sort is not null)
                {
                    var rendered = _transformer.RenderOrderByClause(sort, _mapping, WidgetMapping.ParamPrefix);
                    sql += " ORDER BY " + rendered.orderByClause;
                    Merge(parameters, rendered.parameters);
                }
                else
                {
                    sql += $" ORDER BY {_idColumn}";
                }
            }

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            foreach (var pair in parameters)
            {
                _bind(cmd, pair.Key, pair.Value);
            }

            var ids = new List<int>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                ids.Add(Convert.ToInt32(reader[0]));
            }

            return ids;
        }

        private string BuildLiftedSortSql(SortDirective sort, string whereSql, Dictionary<string, object> parameters)
        {
            var selectParts = new List<string> { $"{_idColumn} AS \"_wid\"" };
            var orderParts = new List<string>();
            for (int i = 0; i < sort.Items.Count; i++)
            {
                var one = new SortDirective(new[] { sort.Items[i] });
                var rendered = _transformer.RenderOrderByClause(one, _mapping, WidgetMapping.ParamPrefix + i);
                var (expr, direction) = SplitOrderItem(rendered.orderByClause);
                selectParts.Add($"{expr} AS \"_s{i}\"");
                orderParts.Add($"\"_s{i}\" {direction}");
                Merge(parameters, rendered.parameters);
            }

            return $"SELECT \"_wid\" FROM (SELECT {string.Join(", ", selectParts)} FROM {_widgetTable}{whereSql}) AS \"_q\" ORDER BY {string.Join(", ", orderParts)}";
        }

        private static (string Expression, string Direction) SplitOrderItem(string orderByClause)
        {
            if (orderByClause.EndsWith(" DESC", StringComparison.Ordinal))
            {
                return (orderByClause.Substring(0, orderByClause.Length - 5), "DESC");
            }

            if (orderByClause.EndsWith(" ASC", StringComparison.Ordinal))
            {
                return (orderByClause.Substring(0, orderByClause.Length - 4), "ASC");
            }

            return (orderByClause, "ASC");
        }

        public IReadOnlyList<string> QueryTagLabels(int widgetId, SortDirective sort)
        {
            var rendered = _transformer.RenderOrderByClause(sort, _tagMapping, WidgetMapping.ParamPrefix);
            var sql = $"SELECT {_labelColumn} FROM {_tagTable} WHERE {_widgetIdColumn} = {FormatInlineId(widgetId)} ORDER BY {rendered.orderByClause}";
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            foreach (var pair in rendered.parameters)
            {
                _bind(cmd, pair.Key, pair.Value);
            }

            var labels = new List<string>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                labels.Add(reader.GetString(0));
            }

            return labels;
        }

        private static string FormatInlineId(int id) => id.ToString(System.Globalization.CultureInfo.InvariantCulture);

        private static void Merge(Dictionary<string, object> target, Dictionary<string, object> extra)
        {
            foreach (var pair in extra)
            {
                target[pair.Key] = pair.Value;
            }
        }
    }
}
