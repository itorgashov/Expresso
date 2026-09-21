using Expresso.Rendering;
using Expresso.Rendering.TestCases;

namespace Expresso.Tests.SqlServer
{
    public sealed class SqlServerRendererParityTests : RendererParityTests
    {
        protected override DialectSql D { get; } = new SqlServerDialectSql();
    }

    file sealed class SqlServerDialectSql : DialectSql
    {
        public override IExpressionToQueryClauseTransformer Transformer { get; } = new ExpressionToSqlServerQueryClauseTransformer();

        public override string Q(string dotted) => SqlQuotes.Brackets(dotted);

        public override string P(int index) => $"@param_{index}";

        public override string Length(string inner) => $"LEN({inner})";

        public override string Left(string inner, string countSql) => $"LEFT({inner}, {countSql})";

        public override string Right(string inner, string countSql) => $"RIGHT({inner}, {countSql})";

        public override string Substring(string inner, string startSql, string lengthSql) =>
            $"SUBSTRING({inner}, {startSql}, {lengthSql})";

        public override string Concat(params string[] parts) => $"CONCAT({string.Join(", ", parts)})";

        public override string IndexOf(string haystack, string needle) =>
            $"(ISNULL(NULLIF(CHARINDEX({needle}, {haystack}), 0), 0) - 1)";

        public override string Ceiling(string inner) => $"CEILING({inner})";

        public override string Mod(string left, string right) => $"({left} % {right})";

        public override string Year(string inner) => $"YEAR({inner})";

        public override string Month(string inner) => $"MONTH({inner})";

        public override string Day(string inner) => $"DAY({inner})";

        public override string DayOfYear(string inner) => $"DATEPART(dayofyear, {inner})";

        public override string Hour(string inner) => $"DATEPART(hour, {inner})";

        public override string Minute(string inner) => $"DATEPART(minute, {inner})";

        public override string Second(string inner) => $"DATEPART(second, {inner})";

        public override string DayOfWeek(string inner) => $"((DATEPART(weekday, {inner}) + @@DATEFIRST - 1) % 7)";

        public override string DateCast(string inner) => $"CAST({inner} AS date)";

        public override string TimeCast(string inner) => $"CAST({inner} AS time)";

        public override string DateAdd(string part, string amountSql, string dateSql) =>
            $"DATEADD({part}, {amountSql}, {dateSql})";
    }
}
