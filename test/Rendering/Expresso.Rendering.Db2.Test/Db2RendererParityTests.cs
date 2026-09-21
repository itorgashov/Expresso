using Expresso.Rendering;
using Expresso.Rendering.TestCases;

namespace Expresso.Tests.Db2
{
    public sealed class Db2RendererParityTests : RendererParityTests
    {
        protected override DialectSql D { get; } = new Db2DialectSql();
    }

    file sealed class Db2DialectSql : DialectSql
    {
        public override IExpressionToQueryClauseTransformer Transformer { get; } = new ExpressionToDb2QueryClauseTransformer();

        public override string Q(string dotted) => SqlQuotes.Double(dotted);

        public override string P(int index) => $"@param_{index}";

        public override string Length(string inner) => $"LENGTH({inner})";

        public override string Left(string inner, string countSql) => $"LEFT({inner}, {countSql})";

        public override string Right(string inner, string countSql) => $"RIGHT({inner}, {countSql})";

        public override string Substring(string inner, string startSql, string lengthSql) =>
            $"SUBSTR({inner}, {startSql}, {lengthSql})";

        public override string Concat(params string[] parts) => $"({string.Join(" || ", parts)})";

        public override string IndexOf(string haystack, string needle) => $"(LOCATE({needle}, {haystack}) - 1)";

        public override string Ceiling(string inner) => $"CEILING({inner})";

        public override string Mod(string left, string right) => $"MOD({left}, {right})";

        public override string Year(string inner) => $"YEAR({inner})";

        public override string Month(string inner) => $"MONTH({inner})";

        public override string Day(string inner) => $"DAY({inner})";

        public override string DayOfYear(string inner) => $"DAYOFYEAR({inner})";

        public override string Hour(string inner) => $"HOUR({inner})";

        public override string Minute(string inner) => $"MINUTE({inner})";

        public override string Second(string inner) => $"SECOND({inner})";

        public override string DayOfWeek(string inner) => $"(DAYOFWEEK({inner}) - 1)";

        public override string DateCast(string inner) => $"CAST({inner} AS date)";

        public override string TimeCast(string inner) => $"CAST({inner} AS time)";

        public override string DateAdd(string part, string amountSql, string dateSql) =>
            $"({dateSql} + {amountSql} {part.ToUpperInvariant()}S)";
    }
}
