#if NETFRAMEWORK
namespace Expresso.Rendering.Integration.Test.Ef6
{
    /// <summary>Kind of documented EF6 gap: the exception type <see cref="Ef6EngineItTests"/> expects.</summary>
    public enum Ef6GapKind
    {
        /// <summary>The renderer must throw <see cref="NotSupportedException"/>.</summary>
        Unsupported,

        /// <summary>The database rejects the query with <see cref="System.Data.Common.DbException"/>.</summary>
        Database,
    }

    /// <summary>A case an EF6 provider cannot render exactly: it must fail loudly, or (<paramref name="Throws"/> false) it silently diverges.</summary>
    public sealed record Ef6Gap(string Reason, bool Throws = true, Ef6GapKind Kind = Ef6GapKind.Unsupported);

    /// <summary>Documented EF6 provider gaps by case id (catalog and differential ids share one namespace).</summary>
    internal static class Ef6ProviderGaps
    {
        private static readonly string[] TimeOfDayCases =
            { "eq-time", "eq-time-midnight", "time", "time-hour", "time-wrap", "time-carry", "time-negative", "time-seconds", "time-add-eq" };

        private static readonly string[] DateAddCases =
            { "addyears", "addmonths", "adddays", "addhours", "addhours-neg", "addminutes", "addseconds" };

        private static readonly (string[] Cases, Ef6Gap Gap) MonthAdds =
            (new[] { "addyears", "addmonths" }, new Ef6Gap("MySQL adds months only with INTERVAL syntax, which no store function can express"));

        public static readonly IReadOnlyDictionary<string, Ef6Gap> None = new Dictionary<string, Ef6Gap>();

        public static readonly IReadOnlyDictionary<string, Ef6Gap> PostgreSql = Build(
            (new[] { "round", "round-digits", "round-half", "round-half-digits" }, new Ef6Gap("EF6 cannot cast to numeric, and PostgreSQL rounds double precision half to even")),
            (new[] { "sqrt", "isnull-sqrt-neg" }, new Ef6Gap("the provider exposes no store functions")));

        public static readonly IReadOnlyDictionary<string, Ef6Gap> MariaDb = Build(MonthAdds);

        public static readonly IReadOnlyDictionary<string, Ef6Gap> MySql = Build(
            MonthAdds,
            (new[] { "time", "time-add-eq" }, new Ef6Gap(
                "MySql.Data sends TimeSpan parameters as '0 hh:mm:ss.ffffff', which MySQL 8 compares with a computed TIME as text, so no row matches",
                Throws: false)));

        public static readonly IReadOnlyDictionary<string, Ef6Gap> Oracle = Build(
            (new[] { "power", "sort-power-subnormal", "isnull-power-subnormal-nullable" }, new Ef6Gap("integer-to-double promotion changes Oracle NUMBER arithmetic to BINARY_DOUBLE")),
            (TimeOfDayCases, new Ef6Gap("the provider has no time-of-day (Edm.Time) type")),
            (DateAddCases, new Ef6Gap("the provider pastes the amount into an INTERVAL literal, so parameters fail (ORA-01867)")),
            (new[] { "concat", "concat-null-eq", "right-trailing" }, new Ef6Gap(
                "EF6's concat null guard emits N'' (ORA-12704 on VARCHAR2 columns)",
                Kind: Ef6GapKind.Database)),
            (new[] { "sqrt", "isnull-sqrt-neg" }, new Ef6Gap("the provider manifest has no SQRT")));

        public static readonly IReadOnlyDictionary<string, Ef6Gap> Sqlite = Build(
            (TimeOfDayCases, new Ef6Gap("the provider has no time-of-day (Edm.Time) type")),
            (DateAddCases, new Ef6Gap("the provider translates no canonical date arithmetic")),
            (new[] { "date" }, new Ef6Gap("the provider does not translate TruncateTime")));

        private static IReadOnlyDictionary<string, Ef6Gap> Build(params (string[] Cases, Ef6Gap Gap)[] groups) =>
            groups.SelectMany(g => g.Cases.Select(id => (id, g.Gap))).ToDictionary(p => p.id, p => p.Gap);
    }
}
#endif
