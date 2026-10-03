using System.Linq.Expressions;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Filtering;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.EntityFrameworkCore.Test
{
    /// <summary>Provider selection and the shape of overridden hooks (no EF provider needed).</summary>
    public class EfCoreExpressionToLinqTransformerTests
    {
        [Theory]
        [InlineData("Microsoft.EntityFrameworkCore.SqlServer", EfCoreProvider.SqlServer)]
        [InlineData("Npgsql.EntityFrameworkCore.PostgreSQL", EfCoreProvider.PostgreSql)]
        [InlineData("Pomelo.EntityFrameworkCore.MySql", EfCoreProvider.MySql)]
        [InlineData("MySql.EntityFrameworkCore", EfCoreProvider.MySql)]
        [InlineData("Microsoft.EntityFrameworkCore.Sqlite", EfCoreProvider.Sqlite)]
        [InlineData("Oracle.EntityFrameworkCore", EfCoreProvider.Oracle)]
        [InlineData("IBM.EntityFrameworkCore", EfCoreProvider.Db2)]
        [InlineData("Microsoft.EntityFrameworkCore.InMemory", EfCoreProvider.Other)]
        [InlineData(null, EfCoreProvider.Other)]
        public void Constructor_ProviderName_ResolvesProvider(string? providerName, EfCoreProvider expected)
        {
            Assert.Equal(expected, EfCoreProviders.Resolve(providerName));
            Assert.Equal(expected, new EfCoreExpressionToLinqTransformer(providerName).Provider);
        }

        [Theory]
        [MemberData(nameof(RendererIntegrationCases.FilterCases), MemberType = typeof(RendererIntegrationCases))]
        public void OtherProvider_AnyCatalogFilter_UsesNoMarkers(FilterCase testCase)
        {
            Assert.Empty(MarkerCalls(Predicate(EfCoreProvider.Other, testCase.Filter)));
        }

        [Theory]
        [InlineData(EfCoreProvider.Db2, "addmonths", "AddMonths")]
        [InlineData(EfCoreProvider.Db2, "indexof", "IndexOf")]
        [InlineData(EfCoreProvider.SqlServer, "indexof", "IndexOf")]
        [InlineData(EfCoreProvider.Oracle, "indexof-empty", "IndexOf")]
        [InlineData(EfCoreProvider.Db2, "round-digits", "Round")]
        [InlineData(EfCoreProvider.Oracle, "date", "Date")]
        [InlineData(EfCoreProvider.Oracle, "dayofweek", "DayOfWeek")]
        [InlineData(EfCoreProvider.PostgreSql, "round", "Round")]
        [InlineData(EfCoreProvider.MySql, "time-hour", "Hour")]
        [InlineData(EfCoreProvider.MySql, "time-carry", "AddMinutes")]
        public void Provider_OverriddenFunction_CallsMarker(EfCoreProvider provider, string caseId, string marker)
        {
            Assert.Contains(marker, MarkerCalls(Predicate(provider, Filter(caseId))).Select(m => m.Method.Name));
        }

        [Theory]
        [InlineData(EfCoreProvider.SqlServer, true)]
        [InlineData(EfCoreProvider.PostgreSql, true)]
        [InlineData(EfCoreProvider.MySql, false)]
        [InlineData(EfCoreProvider.Sqlite, false)]
        [InlineData(EfCoreProvider.Db2, false)]
        public void Concat_NullAsEmpty_OnlyWhereTheDialectIgnoresNull(EfCoreProvider provider, bool nullAsEmpty)
        {
            var predicate = Predicate(provider, Filter("concat-null-isnull"));

            // NULL-as-empty makes the concat never NULL, so isnull(concat(...)) folds to a constant false.
            Assert.Equal(nullAsEmpty, predicate.Body is ConstantExpression { Value: false });
        }

        [Theory]
        [InlineData(EfCoreProvider.SqlServer, false)]
        [InlineData(EfCoreProvider.PostgreSql, false)]
        [InlineData(EfCoreProvider.Oracle, true)]
        [InlineData(EfCoreProvider.Sqlite, true)]
        public void Concat_AllArgumentsNull_IsNullWhereTheDialectReturnsNull(EfCoreProvider provider, bool canBeNull)
        {
            var predicate = Predicate(provider, Filter("concat-null-all"));

            Assert.Equal(canBeNull, predicate.Body is not ConstantExpression);
        }

        [Theory]
        [InlineData(EfCoreProvider.SqlServer, true)]
        [InlineData(EfCoreProvider.Db2, true)]
        [InlineData(EfCoreProvider.PostgreSql, false)]
        [InlineData(EfCoreProvider.Oracle, false)]
        public void Average_OfInteger_IsTruncatedOnlyOnIntegerAverageEngines(EfCoreProvider provider, bool truncated)
        {
            var predicate = Predicate(provider, Filter("avg-fraction"));

            var toInt = Find<UnaryExpression>(predicate, u => u.NodeType == ExpressionType.Convert && u.Type == typeof(int?));
            Assert.Equal(truncated, toInt.Any(u => u.Operand.Type == typeof(double?)));
        }

        [Fact]
        public void Average_OfDouble_IsNeverTruncated()
        {
            var score = new Field("score", typeof(int), "tags");
            var average = new CollectionAvgFunc(new CollectionRef("tags"), new MultFunc(score, new Literal(1.0)));
            var filter = new FilterCriteria { Expression = new GtFunc(average, new Literal(3.0)) };

            var predicate = Predicate(EfCoreProvider.SqlServer, filter);

            Assert.DoesNotContain(Find<UnaryExpression>(predicate, u => u.NodeType == ExpressionType.Convert), u => u.Type == typeof(int?) && u.Operand.Type == typeof(double?));
        }

        private static Expression<Func<Widget, bool>> Predicate(EfCoreProvider provider, FilterCriteria filter) =>
            new EfCoreExpressionToLinqTransformer(provider).BuildPredicate(filter, WidgetLinqMapping.Create());

        private static FilterCriteria Filter(string caseId) =>
            RendererIntegrationCases.AllFilters().SingleOrDefault(c => c.Id == caseId)?.Filter
            ?? RendererDifferentialCases.All().Single(c => c.Id == caseId).Filter!;

        private static IReadOnlyList<MethodCallExpression> MarkerCalls(Expression expression) =>
            Find<MethodCallExpression>(expression, c => c.Method.DeclaringType == typeof(ExpressoDbFunctions));

        private static IReadOnlyList<T> Find<T>(Expression expression, Func<T, bool> match)
            where T : Expression
        {
            var collector = new Collector<T>(match);
            collector.Visit(expression);
            return collector.Found;
        }

        private sealed class Collector<T> : ExpressionVisitor
            where T : Expression
        {
            private readonly Func<T, bool> _match;

            public Collector(Func<T, bool> match)
            {
                _match = match;
            }

            public List<T> Found { get; } = new();

            public override Expression? Visit(Expression? node)
            {
                if (node is T typed && _match(typed))
                {
                    Found.Add(typed);
                }

                return base.Visit(node);
            }
        }
    }
}
