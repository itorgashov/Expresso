using Expresso.Rendering;
using Expresso.Rendering.TestCases;

namespace Expresso.Tests.Oracle
{
    public sealed class OracleRendererParityTests : RendererParityTests
    {
        protected override DialectSql D { get; } = new OracleDialectSql();
    }

    file sealed class OracleDialectSql : DialectSql
    {
        public override IExpressionToQueryClauseTransformer Transformer { get; } = new ExpressionToOracleQueryClauseTransformer();

        public override string Q(string dotted) => SqlQuotes.Double(dotted);

        public override string P(int index) => $":param_{index}";

        public override string Length(string inner) => $"LENGTH({inner})";

        public override string Left(string inner, string countSql) => $"SUBSTR({inner}, 1, {countSql})";

        public override string Right(string inner, string countSql) => $"SUBSTR({inner}, -({countSql}))";

        public override string Substring(string inner, string startSql, string lengthSql) =>
            $"SUBSTR({inner}, {startSql}, {lengthSql})";

        public override string Concat(params string[] parts) => $"({string.Join(" || ", parts)})";

        public override string IndexOf(string haystack, string needle) => $"(INSTR({haystack}, {needle}) - 1)";

        public override string Ceiling(string inner) => $"CEIL({inner})";

        public override string Mod(string left, string right) => $"MOD({left}, {right})";

        public override string Year(string inner) => $"EXTRACT(YEAR FROM {inner})";

        public override string Month(string inner) => $"EXTRACT(MONTH FROM {inner})";

        public override string Day(string inner) => $"EXTRACT(DAY FROM {inner})";

        public override string DayOfYear(string inner) => $"TO_NUMBER(TO_CHAR({inner}, 'DDD'))";

        public override string Hour(string inner) => $"EXTRACT(HOUR FROM {inner})";

        public override string Minute(string inner) => $"EXTRACT(MINUTE FROM {inner})";

        public override string Second(string inner) => $"EXTRACT(SECOND FROM {inner})";

        public override string DayOfWeek(string inner) => $"(TO_NUMBER(TO_CHAR({inner}, 'D')) - 1)";

        public override string DateCast(string inner) => $"TRUNC({inner})";

        public override string TimeCast(string inner) => $"({inner} - TRUNC({inner}))";

        public override string DateAdd(string part, string amountSql, string dateSql)
        {
            if (part is "year" or "month")
            {
                return $"({dateSql} + NUMTOYMINTERVAL({amountSql}, '{part.ToUpperInvariant()}'))";
            }

            return $"({dateSql} + NUMTODSINTERVAL({amountSql}, '{part.ToUpperInvariant()}'))";
        }
    }
}
