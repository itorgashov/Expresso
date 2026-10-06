#if NETFRAMEWORK
namespace Expresso.Rendering.Integration.Test.Ef6
{
    /// <summary>
    /// The shared catalog through EF6 on each engine's seeded database (EF6 = expected ids), plus the differential
    /// cases (EF6 = ADO on the same engine). Documented provider gaps must fail loudly instead. No DB2: IBM's EF6
    /// provider is not on NuGet.
    /// </summary>
    public abstract class Ef6EngineItTests : EngineItTests
    {
        protected Ef6EngineItTests(IEngineSession? ado, IEngineSession? ef6, IReadOnlyDictionary<string, Ef6Gap> gaps)
        {
            AdoSession = ado!;
            Session = ef6!;
            Gaps = gaps;
        }

        protected IEngineSession AdoSession { get; }

        protected override IEngineSession Session { get; }

        private IReadOnlyDictionary<string, Ef6Gap> Gaps { get; }

        [SkippableTheory]
        [MemberData(nameof(RendererDifferentialCases.Cases), MemberType = typeof(RendererDifferentialCases))]
        public void Differential_Ef6MatchesAdo(DifferentialCase testCase)
        {
            Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
            if (AssertedGap(testCase.Id, () => Session.QueryWidgetIds(testCase.Filter, testCase.Sort)))
            {
                return;
            }

            var ado = DifferentialOutcome.Of(() => AdoSession.QueryWidgetIds(testCase.Filter, testCase.Sort));
            var ef6 = DifferentialOutcome.Of(() => Session.QueryWidgetIds(testCase.Filter, testCase.Sort));
            DifferentialOutcome.AssertSame(
                ado,
                ef6,
                testCase.Rejection,
                () => ((Ef6EngineSession)Session).Sql(testCase.Filter, testCase.Sort),
                testCase.UnsupportedReason,
                testCase.DatabaseCodes);
        }

        protected override void AssertOutcome<T>(string caseId, IReadOnlyList<T> expected, Func<IReadOnlyList<T>> query)
        {
            if (!AssertedGap(caseId, () => query()))
            {
                base.AssertOutcome(caseId, expected, query);
            }
        }

        /// <summary>True when <paramref name="caseId"/> is a documented gap: it must throw, or it is skipped when it diverges silently.</summary>
        private bool AssertedGap(string caseId, Action query)
        {
            if (!Gaps.TryGetValue(caseId, out var gap))
            {
                return false;
            }

            Skip.IfNot(gap.Throws, "Known silent EF6 divergence: " + gap.Reason);
            var ex = Record.Exception(query);
            Assert.NotNull(ex);
            Ef6GapAssertions.Assert(gap, ex!);
            return true;
        }
    }

    [Trait("Category", "Integration")]
    [Collection("SqlServerIT")]
    public sealed class SqlServerEf6ItTests : Ef6EngineItTests
    {
        public SqlServerEf6ItTests(SqlServerItFixture f) : base(f.Session, f.Ef6Session, Ef6ProviderGaps.None) { }
    }

    [Trait("Category", "Integration")]
    [Collection("PostgreSqlIT")]
    public sealed class PostgreSqlEf6ItTests : Ef6EngineItTests
    {
        public PostgreSqlEf6ItTests(PostgreSqlItFixture f) : base(f.Session, f.Ef6Session, Ef6ProviderGaps.PostgreSql) { }
    }

    [Trait("Category", "Integration")]
    [Collection("SqliteIT")]
    public sealed class SqliteEf6ItTests : Ef6EngineItTests
    {
        public SqliteEf6ItTests(SqliteItFixture f) : base(f.Session, f.Ef6Session, Ef6ProviderGaps.Sqlite) { }
    }

    [Trait("Category", "Integration")]
    [Collection("MySqlIT")]
    public sealed class MySqlEf6ItTests : Ef6EngineItTests
    {
        public MySqlEf6ItTests(MySqlItFixture f) : base(f.Session, f.Ef6Session, Ef6ProviderGaps.MySql) { }
    }

    [Trait("Category", "Integration")]
    [Collection("MariaDbIT")]
    public sealed class MariaDbEf6ItTests : Ef6EngineItTests
    {
        public MariaDbEf6ItTests(MariaDbItFixture f) : base(f.Session, f.Ef6Session, Ef6ProviderGaps.MariaDb) { }
    }

    [Trait("Category", "Integration")]
    [Collection("OracleIT")]
    public sealed class OracleEf6ItTests : Ef6EngineItTests
    {
        public OracleEf6ItTests(OracleItFixture f) : base(f.Session, f.Ef6Session, Ef6ProviderGaps.Oracle) { }
    }
}
#endif
