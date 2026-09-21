namespace Expresso.Rendering.TestCases
{
    /// <summary>
    /// Dialect-specific SQL fragments for shared renderer unit tests. IR lives in the tests; goldens live here as hooks.
    /// </summary>
    public abstract class DialectSql
    {
        public abstract IExpressionToQueryClauseTransformer Transformer { get; }

        public virtual string Prefix => RendererMaps.ParamPrefix;

        public abstract string Q(string dotted);

        public abstract string P(int index);

        public abstract string Length(string inner);

        public abstract string Left(string inner, string countSql);

        public abstract string Right(string inner, string countSql);

        public abstract string Substring(string inner, string startSql, string lengthSql);

        public abstract string Concat(params string[] parts);

        public abstract string IndexOf(string haystack, string needle);

        public abstract string Ceiling(string inner);

        public abstract string Mod(string left, string right);

        public abstract string Year(string inner);

        public abstract string Month(string inner);

        public abstract string Day(string inner);

        public abstract string DayOfYear(string inner);

        public abstract string Hour(string inner);

        public abstract string Minute(string inner);

        public abstract string Second(string inner);

        public abstract string DayOfWeek(string inner);

        public abstract string DateCast(string inner);

        public abstract string TimeCast(string inner);

        public abstract string DateAdd(string part, string amountSql, string dateSql);

        public virtual string Floor(string inner) => $"FLOOR({inner})";

        public virtual string Sqrt(string inner) => $"SQRT({inner})";

        public virtual string Sign(string inner) => $"SIGN({inner})";

        public virtual string Power(string inner, string exp) => $"POWER({inner}, {exp})";

        public virtual string Round(string inner, string digits) => $"ROUND({inner}, {digits})";

        /// <summary>SQL text after the LIKE pattern, including <c>ESCAPE</c>. Default is SQL Server <c>ESCAPE '\'</c>.</summary>
        public virtual string LikeEscape => "ESCAPE '\\'";

        public virtual string Lower(string inner) => $"LOWER({inner})";

        public virtual string Upper(string inner) => $"UPPER({inner})";

        public virtual string Trim(string inner) => $"TRIM({inner})";

        public virtual string LTrim(string inner) => $"LTRIM({inner})";

        public virtual string RTrim(string inner) => $"RTRIM({inner})";

        public virtual string Replace(string inner, string oldSql, string newSql) =>
            $"REPLACE({inner}, {oldSql}, {newSql})";
    }
}
