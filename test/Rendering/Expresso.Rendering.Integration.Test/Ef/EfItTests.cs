#if NET8_0_OR_GREATER
using Expresso.Core.Paging;

namespace Expresso.Rendering.Integration.Test.Ef
{
    /// <summary>
    /// The shared catalog through EF Core on each engine's seeded ADO connection (EF = expected ids), plus the
    /// differential cases (EF = ADO on the same engine).
    /// </summary>
    public abstract class EfEngineItTests : EngineItTests
    {
        protected EfEngineItTests(IEngineSession? ado, IEngineSession? ef)
        {
            AdoSession = ado!;
            Session = ef!;
        }

        protected IEngineSession AdoSession { get; }

        protected override IEngineSession Session { get; }

        [SkippableTheory]
        [MemberData(nameof(RendererDifferentialCases.Cases), MemberType = typeof(RendererDifferentialCases))]
        public void Differential_EfMatchesAdo(DifferentialCase testCase)
        {
            Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
            var ado = DifferentialOutcome.Of(() => AdoSession.QueryWidgetIds(testCase.Filter, testCase.Sort));
            var ef = DifferentialOutcome.Of(() => Session.QueryWidgetIds(testCase.Filter, testCase.Sort));
            DifferentialOutcome.AssertSame(
                ado,
                ef,
                testCase.Rejection,
                () => ((EfEngineSession)Session).Sql(testCase.Filter, testCase.Sort),
                testCase.UnsupportedReason,
                testCase.DatabaseCodes);
        }

        [SkippableTheory]
        [MemberData(nameof(RendererIntegrationCases.NestedSortCases), MemberType = typeof(RendererIntegrationCases))]
        public void NestedSort_IncludeSorted_ReturnsExpectedLabels(NestedSortCase testCase)
        {
            Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
            var labels = ((EfEngineSession)Session).QueryIncludedTagLabels(testCase.WidgetId, testCase.Sort);
            Assert.Equal(testCase.ExpectedLabels, labels);
        }
    }

    [Trait("Category", "Integration")]
    [Collection("SqlServerIT")]
    public sealed class SqlServerEfItTests : EfEngineItTests
    {
        public SqlServerEfItTests(SqlServerItFixture f) : base(f.Session, f.EfSession) { }
    }

    [Trait("Category", "Integration")]
    [Collection("PostgreSqlIT")]
    public sealed class PostgreSqlEfItTests : EfEngineItTests
    {
        public PostgreSqlEfItTests(PostgreSqlItFixture f) : base(f.Session, f.EfSession) { }
    }

    [Trait("Category", "Integration")]
    [Collection("SqliteIT")]
    public sealed class SqliteEfItTests : EfEngineItTests
    {
        public SqliteEfItTests(SqliteItFixture f) : base(f.Session, f.EfSession) { }
    }

    [Trait("Category", "Integration")]
    [Collection("MySqlIT")]
    public sealed class MySqlEfItTests : EfEngineItTests
    {
        public MySqlEfItTests(MySqlItFixture f) : base(f.Session, f.EfSession) { }
    }

    [Trait("Category", "Integration")]
    [Collection("MariaDbIT")]
    public sealed class MariaDbEfItTests : EfEngineItTests
    {
        public MariaDbEfItTests(MariaDbItFixture f) : base(f.Session, f.EfSession) { }
    }

    [Trait("Category", "Integration")]
    [Collection("OracleIT")]
    public sealed class OracleEfItTests : EfEngineItTests
    {
        public OracleEfItTests(OracleItFixture f) : base(f.Session, f.EfSession) { }
    }

    [Trait("Category", "Integration")]
    [Collection("Db2IT")]
    public sealed class Db2EfItTests : EfEngineItTests
    {
        public Db2EfItTests(Db2ItFixture f) : base(f.Session, f.EfSession) { }

        [SkippableFact]
        public void Page_Offset_IsRejected()
        {
            Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
            var error = Assert.Throws<NotSupportedException>(() => ((EfEngineSession)Session).Page(new PagingDirective(page: 2, pageSize: 2)));
            Assert.Contains("OFFSET", error.Message);
        }
    }
}
#endif
