using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using System.Text;

namespace Expresso.SqlServer
{
    public partial class ExpressionToSqlServerQueryClauseTransformer
    {
        public (string whereClause, Dictionary<string, object> parameters) RenderWhereClause(
            FilterCriteria filterCriteria,
            SqlQueryMapping mapping,
            string paramNamePrefix)
        {
            if (filterCriteria is null)
            {
                throw new ArgumentNullException(nameof(filterCriteria));
            }
            if (filterCriteria.Expression is null)
            {
                throw new ArgumentException("The expression of the filter criteria is null.", nameof(filterCriteria));
            }
            if (mapping is null)
            {
                throw new ArgumentNullException(nameof(mapping));
            }

            EnsureParamNamePrefix(paramNamePrefix);

            var sqlBuilder = new StringBuilder();
            var parameters = new Dictionary<string, object>();
            GenerateClause(filterCriteria.Expression, mapping.FieldToColumn, sqlBuilder, parameters, paramNamePrefix, mapping.Collections);
            return (sqlBuilder.ToString(), parameters);
        }

        public (string orderByClause, Dictionary<string, object> parameters) RenderOrderByClause(
            SortDirective sortDirective,
            SqlQueryMapping mapping,
            string paramNamePrefix)
        {
            if (sortDirective == null)
            {
                throw new ArgumentNullException(nameof(sortDirective));
            }
            if (sortDirective.Items == null || sortDirective.Items.Count == 0)
            {
                throw new ArgumentException("Sort directive must contain at least one item", nameof(sortDirective));
            }
            if (mapping is null)
            {
                throw new ArgumentNullException(nameof(mapping));
            }

            EnsureParamNamePrefix(paramNamePrefix);

            var sqlBuilder = new StringBuilder();
            var parameters = new Dictionary<string, object>();

            for (int i = 0; i < sortDirective.Items.Count; i++)
            {
                var item = sortDirective.Items[i];

                if (i > 0)
                {
                    sqlBuilder.Append(", ");
                }

                if (item.Expression is CollectionQuantifierFunction)
                {
                    throw new ArgumentException("Collections and collection quantifiers cannot be used as sort keys.");
                }

                if (item.Expression is BooleanFunction booleanExpression)
                {
                    sqlBuilder.Append("(CASE WHEN ");
                    GenerateClause(booleanExpression, mapping.FieldToColumn, sqlBuilder, parameters, paramNamePrefix, mapping.Collections);
                    sqlBuilder.Append(" THEN 1 ELSE 0 END)");
                }
                else
                {
                    GenerateClause(item.Expression, mapping.FieldToColumn, sqlBuilder, parameters, paramNamePrefix, mapping.Collections);
                }

                sqlBuilder.Append(item.Direction == SortDirection.Ascending ? " ASC" : " DESC");
            }

            return (sqlBuilder.ToString(), parameters);
        }

        private bool TryGenerateCollectionFunction(
            AbstractExpression expression,
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            switch (expression)
            {
                case AnyFunc any:
                    GenerateExistsClause(any.Collection, any.Predicate, negate: false, negatePredicate: false, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case NoneFunc none:
                    GenerateExistsClause(none.Collection, none.Predicate, negate: true, negatePredicate: false, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case AllFunc all:
                    if (all.Predicate is null)
                    {
                        sqlBuilder.Append("(1 = 1)");
                        return true;
                    }

                    GenerateExistsClause(all.Collection, all.Predicate, negate: true, negatePredicate: true, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case CollectionCountFunc count:
                    GenerateCollectionAggregateClause("COUNT(*)", count.Collection, selector: null, count.Predicate, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case CollectionMinFunc min:
                    GenerateCollectionAggregateClause("MIN", min.Collection, min.Selector, predicate: null, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case CollectionMaxFunc max:
                    GenerateCollectionAggregateClause("MAX", max.Collection, max.Selector, predicate: null, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case CollectionSumFunc sum:
                    GenerateCollectionAggregateClause("SUM", sum.Collection, sum.Selector, predicate: null, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                case CollectionAvgFunc avg:
                    GenerateCollectionAggregateClause("AVG", avg.Collection, avg.Selector, predicate: null, sqlBuilder, parameters, paramNamePrefix, collections);
                    return true;
                default:
                    return false;
            }
        }

        private void GenerateExistsClause(
            CollectionRef collection,
            AbstractExpression? predicate,
            bool negate,
            bool negatePredicate,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            var mapping = RequireCollectionMapping(collection, collections);
            if (negate)
            {
                sqlBuilder.Append("NOT ");
            }

            sqlBuilder.Append("EXISTS (SELECT 1 FROM ");
            sqlBuilder.Append(mapping.FromClause);
            sqlBuilder.Append(" WHERE ");
            sqlBuilder.Append(mapping.CorrelateSql);
            if (predicate is not null)
            {
                sqlBuilder.Append(" AND ");
                if (negatePredicate)
                {
                    sqlBuilder.Append("NOT (");
                }

                GenerateClause(predicate, mapping.ItemFieldToColumn, sqlBuilder, parameters, paramNamePrefix, mapping.Nested);
                if (negatePredicate)
                {
                    sqlBuilder.Append(')');
                }
            }

            sqlBuilder.Append(')');
        }

        private void GenerateCollectionAggregateClause(
            string aggregateSql,
            CollectionRef collection,
            AbstractExpression? selector,
            AbstractExpression? predicate,
            StringBuilder sqlBuilder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            var mapping = RequireCollectionMapping(collection, collections);
            sqlBuilder.Append("(SELECT ");
            if (selector is null)
            {
                sqlBuilder.Append(aggregateSql);
            }
            else
            {
                sqlBuilder.Append(aggregateSql);
                sqlBuilder.Append('(');
                GenerateClause(selector, mapping.ItemFieldToColumn, sqlBuilder, parameters, paramNamePrefix, mapping.Nested);
                sqlBuilder.Append(')');
            }

            sqlBuilder.Append(" FROM ");
            sqlBuilder.Append(mapping.FromClause);
            sqlBuilder.Append(" WHERE ");
            sqlBuilder.Append(mapping.CorrelateSql);
            if (predicate is not null)
            {
                sqlBuilder.Append(" AND ");
                GenerateClause(predicate, mapping.ItemFieldToColumn, sqlBuilder, parameters, paramNamePrefix, mapping.Nested);
            }

            sqlBuilder.Append(')');
        }

        private static CollectionSqlMapping RequireCollectionMapping(
            CollectionRef collection,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            if (!collections.TryGetValue(collection.Name, out var mapping))
            {
                throw new ArgumentException($"No mapping for the {collection.Name} collection");
            }

            return mapping;
        }
    }
}
