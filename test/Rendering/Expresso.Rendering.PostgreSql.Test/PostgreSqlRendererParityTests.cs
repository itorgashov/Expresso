using Expresso.Rendering;
using Expresso.Rendering.TestCases;

namespace Expresso.Tests.PostgreSql
{
    public sealed class PostgreSqlRendererParityTests : RendererParityTests
    {
        protected override DialectSql D { get; } = new PostgreSqlDialectSql();
    }

    file sealed class PostgreSqlDialectSql : DialectSql
    {
        public override IExpressionToQueryClauseTransformer Transformer { get; } = new ExpressionToPostgreSqlQueryClauseTransformer();

        public override string Q(string dotted) => SqlQuotes.Double(dotted);

        public override string P(int index) => $"@param_{index}";

        public override string Length(string inner) => $"LENGTH({inner})";

        public override string Left(string inner, string countSql) => $"LEFT({inner}, {countSql})";

        public override string Right(string inner, string countSql) => $"RIGHT({inner}, {countSql})";

        public override string Substring(string inner, string startSql, string lengthSql) =>
            $"SUBSTR({inner}, {startSql}, {lengthSql})";

        public override string Concat(params string[] parts) => $"CONCAT({string.Join(", ", parts)})";

        public override string IndexOf(string haystack, string needle) => $"(STRPOS({haystack}, {needle}) - 1)";

        public override string Ceiling(string inner) => $"CEILING({inner})";

        public override string Round(string inner, string digits) => $"ROUND(CAST({inner} AS numeric), {digits})";

        public override string Mod(string left, string right) => $"({left} % {right})";

        public override string Year(string inner) => $"CAST(EXTRACT(YEAR FROM {inner}) AS integer)";

        public override string Month(string inner) => $"CAST(EXTRACT(MONTH FROM {inner}) AS integer)";

        public override string Day(string inner) => $"CAST(EXTRACT(DAY FROM {inner}) AS integer)";

        public override string DayOfYear(string inner) => $"CAST(EXTRACT(DOY FROM {inner}) AS integer)";

        public override string Hour(string inner) => $"CAST(EXTRACT(HOUR FROM {inner}) AS integer)";

        public override string Minute(string inner) => $"CAST(EXTRACT(MINUTE FROM {inner}) AS integer)";

        public override string Second(string inner) => $"CAST(EXTRACT(SECOND FROM {inner}) AS integer)";

        public override string DayOfWeek(string inner) => $"CAST(EXTRACT(DOW FROM {inner}) AS integer)";

        public override string DateCast(string inner) => $"CAST({inner} AS date)";

        public override string TimeCast(string inner) => $"CAST({inner} AS time)";

        public override string DateAdd(string part, string amountSql, string dateSql) =>
            $"({dateSql} + (({amountSql}) * INTERVAL '1 {part}'))";
    }
}
