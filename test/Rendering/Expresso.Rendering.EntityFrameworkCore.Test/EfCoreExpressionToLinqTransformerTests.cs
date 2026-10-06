using System.Linq.Expressions;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
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
        public void Oracle_IndexOf_BuildsWithoutStringNullMarker()
        {
            var ordinary = Predicate(EfCoreProvider.Oracle, Filter("indexof-case"));
            var empty = Predicate(EfCoreProvider.Oracle, Filter("indexof-empty"));

            Assert.Contains("IndexOf", MarkerCalls(ordinary).Select(m => m.Method.Name));
            Assert.True(empty.Body is ConstantExpression { Value: false });
            Assert.DoesNotContain(MarkerCalls(ordinary), m => m.Method.Name == nameof(ExpressoDbFunctions.IsNullValue));
        }

        [Theory]
        [InlineData("contains-empty")]
        [InlineData("startswith-empty")]
        [InlineData("endswith-empty")]
        public void Oracle_EmptySearchLiteral_IsNotConstantFalse(string caseId)
        {
            var predicate = Predicate(EfCoreProvider.Oracle, Filter(caseId));

            Assert.False(predicate.Body is ConstantExpression { Value: false });
        }

        [Fact]
        public void Sqlite_DivisionByZero_IsNull()
        {
            var amount = new Field("amount", typeof(double));
            var filter = new FilterCriteria { Expression = new IsNullFunc(new DivFunc(amount, new Literal(0.0))) };

            var sqlite = Predicate(EfCoreProvider.Sqlite, filter);
            var mysql = Predicate(EfCoreProvider.MySql, filter);
            var sqlServer = Predicate(EfCoreProvider.SqlServer, filter);

            Assert.Contains(Find<BinaryExpression>(sqlite, e => e.NodeType == ExpressionType.Equal), e => IsZero(e.Left) || IsZero(e.Right));
            Assert.Contains(Find<BinaryExpression>(mysql, e => e.NodeType == ExpressionType.Equal), e => IsZero(e.Left) || IsZero(e.Right));
            Assert.Contains(nameof(ExpressoDbFunctions.IsDomainNull), MarkerCalls(sqlServer).Select(m => m.Method.Name));
        }

        [Fact]
        public void SqlServer_IsNullSqrt_KeepsTheCall()
        {
            using var context = TestWidgetContext.SqlServer();
            var filter = new FilterCriteria { Expression = new IsNullFunc(new SqrtFunc(new Field("amount", typeof(double)))) };

            var sql = context.WhereSql(filter);

            Assert.Contains("SQRT", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("NULLIF", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SqlServer_IsNullDiv_KeepsTheDivision()
        {
            using var context = TestWidgetContext.SqlServer();
            var filter = new FilterCriteria { Expression = new IsNullFunc(new DivFunc(new Field("amount", typeof(double)), new Literal(0.0))) };

            var sql = context.WhereSql(filter);

            Assert.Contains("/", sql);
            Assert.Contains("NULLIF", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Oracle_EmptyScalar_IsNull()
        {
            Assert.True(Predicate(EfCoreProvider.Oracle, new FilterCriteria { Expression = new IsNullFunc(new ConcatFunc(new List<AbstractExpression> { new Literal(""), new Literal("") })) }).Body is ConstantExpression { Value: true });
            Assert.True(Predicate(EfCoreProvider.Oracle, new FilterCriteria { Expression = new IsNullFunc(new LenFunc(new Literal(""))) }).Body is ConstantExpression { Value: true });
            Assert.True(Predicate(EfCoreProvider.Oracle, new FilterCriteria { Expression = new IsNullFunc(new LowerFunc(new Literal(""))) }).Body is ConstantExpression { Value: true });
            Assert.True(Predicate(EfCoreProvider.Oracle, new FilterCriteria { Expression = new IsNullFunc(new IndexOfFunc(new Field("name", typeof(string)), new Literal(""))) }).Body is ConstantExpression { Value: true });
        }

        [Fact]
        public void Oracle_MixedEmptyConcat_IsNullOnlyWhenEveryOperandIsNull()
        {
            var name = new Field("name", typeof(string));
            Assert.True(Predicate(EfCoreProvider.Oracle, new FilterCriteria { Expression = new IsNullFunc(new ConcatFunc(new List<AbstractExpression> { new Literal("a"), new Literal("") })) }).Body is ConstantExpression { Value: false });
            Assert.False(Predicate(EfCoreProvider.Oracle, new FilterCriteria { Expression = new EqFunc(new ConcatFunc(new List<AbstractExpression> { new Literal("a"), new Literal("") }), new Literal("a")) }).Body is ConstantExpression { Value: false });
            Assert.False(Predicate(EfCoreProvider.Oracle, new FilterCriteria { Expression = new IsNullFunc(new ConcatFunc(new List<AbstractExpression> { name, new Literal("") })) }).Body is ConstantExpression);
            Assert.False(Predicate(EfCoreProvider.Oracle, new FilterCriteria { Expression = new EqFunc(new ConcatFunc(new List<AbstractExpression> { name, new Literal("") }), new Literal("Alice")) }).Body is ConstantExpression { Value: false });
            Assert.False(Predicate(EfCoreProvider.Oracle, new FilterCriteria { Expression = new IsNullFunc(new LenFunc(new ConcatFunc(new List<AbstractExpression> { name, new Literal("") }))) }).Body is ConstantExpression);
        }

        [Fact]
        public void SqlServer_LiteralDivision_StaysInSql()
        {
            using var context = TestWidgetContext.SqlServer();
            var divided = new FilterCriteria { Expression = new EqFunc(new DivFunc(new Literal(1.0), new Literal(0.0)), new Literal(0.0)) };
            var nested = new FilterCriteria { Expression = new EqFunc(new DivFunc(new AddFunc(new Literal(1.0), new Literal(2.0)), new Literal(0.0)), new Literal(0.0)) };
            var sqrt = new FilterCriteria { Expression = new IsNullFunc(new SqrtFunc(new Literal(-1.0))) };

            var dividedSql = context.WhereSql(divided);
            var nestedSql = context.WhereSql(nested);
            var sqrtSql = context.WhereSql(sqrt);

            Assert.Contains("/", dividedSql);
            Assert.DoesNotContain("0 = 1", dividedSql);
            Assert.Contains("/", nestedSql);
            Assert.Contains("SQRT", sqrtSql, StringComparison.OrdinalIgnoreCase);

            var left = context.WhereSql(new FilterCriteria { Expression = new EqFunc(new LeftFunc(new Literal("a"), new Literal(100)), new Literal("a")) });
            Assert.Contains("SUBSTRING", left, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Oracle_Replace_EmptySearchOrReplacement_KeepsTheCall()
        {
            using var context = TestWidgetContext.Oracle();
            var name = new Field("name", typeof(string));
            var notes = new Field("notes", typeof(string));

            AssertReplace(context, new IsNullFunc(new ReplaceFunc(name, new Literal("a"), new Literal(""))));
            AssertReplace(context, new EqFunc(new ReplaceFunc(name, new Literal("a"), new Literal("")), new Literal("Bob")));
            AssertReplace(context, new IsNullFunc(new ReplaceFunc(name, new Literal(""), new Literal("x"))));
            AssertReplace(context, new EqFunc(new ReplaceFunc(name, new Literal(""), new Literal("x")), new Literal("Bob")));
            AssertReplace(context, new IsNullFunc(new ReplaceFunc(name, new Literal("Bob"), new Literal(""))));
            var nullSearch = context.WhereSql(new FilterCriteria { Expression = new EqFunc(new ReplaceFunc(name, notes, new Literal("x")), new Literal("Bob")) });
            var nullReplacement = context.WhereSql(new FilterCriteria { Expression = new IsNullFunc(new ReplaceFunc(name, new Literal("a"), notes)) });
            Assert.Contains("REPLACE", nullSearch, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("REPLACE", nullReplacement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Notes\" IS NULL", nullSearch);
            Assert.DoesNotContain("Notes\" IS NULL", nullReplacement);
            Assert.DoesNotContain("0 = 1", nullSearch);

            var emptySource = context.WhereSql(new FilterCriteria { Expression = new IsNullFunc(new ReplaceFunc(new Literal(""), new Literal("a"), new Literal("b"))) });
            Assert.DoesNotContain("REPLACE", emptySource, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("0 = 1", emptySource);
        }

        private static void AssertReplace(TestWidgetContext context, BooleanFunction filter)
        {
            var sql = context.WhereSql(new FilterCriteria { Expression = filter });
            Assert.Contains("REPLACE", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("0 = 1", sql);
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

        private static bool IsZero(Expression expression) =>
            expression is ConstantExpression { Value: double value } && value == 0;

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
