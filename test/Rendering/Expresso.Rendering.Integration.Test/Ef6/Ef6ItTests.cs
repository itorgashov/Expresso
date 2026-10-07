#if NETFRAMEWORK
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Filtering;

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

        [SkippableTheory]
        [InlineData(0.5, 1023.0, true)]
        [InlineData(0.5, 1073.0, true)]
        [InlineData(0.5, 1074.0, true)]
        [InlineData(-0.5, 1073.0, true)]
        [InlineData(0.5, 1075.0, false)]
        [InlineData(0.5, 1074.9999999999998, true)]
        [InlineData(0.5, 1075.0000000000002, false)]
        [InlineData(0.5000000000000001, 1075.0, true)]
        [InlineData(0.49999999999999994, 1075.0, false)]
        [InlineData(2.0, -1075.0, false)]
        [InlineData(2.0, -1074.9999999999998, true)]
        [InlineData(2.0, -1075.0000000000002, false)]
        [InlineData(0.25, 537.0, true)]
        [InlineData(0.25, 537.5, false)]
        [InlineData(0.25, 537.4999999999999, true)]
        public void PowerUnderflowBoundary_MatchesNativeSqlite(double value, double exponent, bool nonzero)
        {
            // Scenario: valid subnormals and immediate neighbors of the half-subnormal boundary must match native SQLite.
            // Both sessions query the shared seed; the filter matches every id only when the literal power is nonzero.
            Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
            var filter = new FilterCriteria
            {
                Expression = new NotFunc(new EqFunc(new PowerFunc(new Literal(value), new Literal(exponent)), new Literal(0.0))),
            };
            var expected = nonzero ? WidgetSeedData.CreateWidgets().Select(w => w.Id).ToArray() : Array.Empty<int>();
            Assert.Equal(expected, AdoSession.QueryWidgetIds(filter, null));
            Assert.Equal(expected, Session.QueryWidgetIds(filter, null));
        }

        [SkippableFact]
        public void PowerUnderflowBoundary_NonBinaryBasesMatchNativeSqlite()
        {
            // Scenario: rounding the half-subnormal boundary with non-binary bases must not discard a nonzero result.
            // Compare native ADO and EF6 at the estimated boundary and its two immediate neighbors on either side.
            Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
            foreach (var value in new[] { 0.3, 0.4, 0.6, 0.7, 0.8, 0.9, 1.1, 1.3, 1.7, 3.0, 5.0, 10.0 })
            {
                var boundary = -1075.0 / (Math.Log(value) / Math.Log(2.0));
                var bits = BitConverter.DoubleToInt64Bits(boundary);
                for (var offset = -2; offset <= 2; offset++)
                {
                    var exponent = BitConverter.Int64BitsToDouble(bits + offset);
                    var filter = new FilterCriteria
                    {
                        Expression = new NotFunc(new EqFunc(new PowerFunc(new Literal(value), new Literal(exponent)), new Literal(0.0))),
                    };
                    var ado = AdoSession.QueryWidgetIds(filter, null);
                    var ef6 = Session.QueryWidgetIds(filter, null);
                    Assert.True(ado.SequenceEqual(ef6), $"POWER({value:R}, {exponent:R}): ADO=[{string.Join(",", ado)}], EF6=[{string.Join(",", ef6)}]");
                }
            }
        }

        [SkippableFact]
        public void PowerUnderflowBoundary_BinaryBasesMatchNativeSqlite()
        {
            // Scenario: powers of two include exact halfway ties and rounded exponents which only look like a tie.
            // Probe both signs of the base's binary exponent, plus neighboring double exponents, against native ADO.
            Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
            foreach (var magnitude in new[] { 1, 3, 5, 8, 25, 43, 100, 215, 512, 688, 860, 1023 })
            {
                foreach (var binaryExponent in new[] { magnitude, -magnitude })
                {
                    var value = Math.Pow(2.0, binaryExponent);
                    var bits = BitConverter.DoubleToInt64Bits(-1075.0 / binaryExponent);
                    for (var offset = -1; offset <= 1; offset++)
                    {
                        var exponent = BitConverter.Int64BitsToDouble(bits + offset);
                        var filter = new FilterCriteria
                        {
                            Expression = new NotFunc(new EqFunc(new PowerFunc(new Literal(value), new Literal(exponent)), new Literal(0.0))),
                        };
                        var ado = AdoSession.QueryWidgetIds(filter, null);
                        var ef6 = Session.QueryWidgetIds(filter, null);
                        Assert.True(ado.SequenceEqual(ef6), $"POWER(2^{binaryExponent}, {exponent:R}): ADO=[{string.Join(",", ado)}], EF6=[{string.Join(",", ef6)}]");
                    }
                }
            }
        }
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
