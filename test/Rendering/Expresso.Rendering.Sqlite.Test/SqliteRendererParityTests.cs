using Expresso.Rendering;
using Expresso.Rendering.TestCases;

namespace Expresso.Tests.Sqlite
{
    public sealed class SqliteRendererParityTests : RendererParityTests
    {
        protected override DialectSql D { get; } = new SqliteDialectSql();
    }

    file sealed class SqliteDialectSql : DialectSql
    {
        public override IExpressionToQueryClauseTransformer Transformer { get; } = new ExpressionToSqliteQueryClauseTransformer();

        public override string Q(string dotted) => SqlQuotes.Double(dotted);

        public override string P(int index) => $"@param_{index}";

        public override string Length(string inner) => $"LENGTH({inner})";

        public override string Left(string inner, string countSql) => $"substr({inner}, 1, {countSql})";

        public override string Right(string inner, string countSql) => $"substr({inner}, -({countSql}))";

        public override string Substring(string inner, string startSql, string lengthSql) =>
            $"substr({inner}, {startSql}, {lengthSql})";

        public override string Concat(params string[] parts) => $"({string.Join(" || ", parts)})";

        public override string IndexOf(string haystack, string needle) => $"(INSTR({haystack}, {needle}) - 1)";

        public override string Ceiling(string inner) => $"CEIL({inner})";

        public override string Mod(string left, string right) => $"({left} % {right})";

        public override string Year(string inner) => $"CAST(strftime('%Y', {inner}) AS integer)";

        public override string Month(string inner) => $"CAST(strftime('%m', {inner}) AS integer)";

        public override string Day(string inner) => $"CAST(strftime('%d', {inner}) AS integer)";

        public override string DayOfYear(string inner) => $"CAST(strftime('%j', {inner}) AS integer)";

        public override string Hour(string inner) => $"CAST(strftime('%H', {inner}) AS integer)";

        public override string Minute(string inner) => $"CAST(strftime('%M', {inner}) AS integer)";

        public override string Second(string inner) => $"CAST(strftime('%S', {inner}) AS integer)";

        public override string DayOfWeek(string inner) => $"CAST(strftime('%w', {inner}) AS integer)";

        public override string DateCast(string inner) => $"date({inner})";

        public override string TimeCast(string inner) => $"time({inner})";

        public override string DateAdd(string part, string amountSql, string dateSql)
        {
            string unit = part + "s";
            return $"datetime({dateSql}, (({amountSql}) || ' {unit}'))";
        }
    }
}
