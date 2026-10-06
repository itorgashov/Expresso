using System.Data.Entity;

namespace Expresso.Rendering.EntityFramework
{
    /// <summary>
    /// EF6 function stubs that <see cref="DbFunctions"/> does not expose: <c>Edm</c> canonical functions and provider
    /// store functions (namespace = the provider manifest's). Only valid inside LINQ to Entities queries.
    /// </summary>
    internal static class Ef6Functions
    {
        [DbFunction("Edm", "DayOfYear")]
        public static int? DayOfYear(DateTime? value) => throw NotInMemory();

        [DbFunction("SqlServer", "SQRT")]
        public static double? SqlServerSqrt(double? value) => throw NotInMemory();

        [DbFunction("SqlServer", "DATALENGTH")]
        public static int? SqlServerDataLength(string value) => throw NotInMemory();

        [DbFunction("SQLite", "SQRT")]
        public static double? SqliteSqrt(double? value) => throw NotInMemory();

        [DbFunction("SQLite", "DATEPART")]
        public static int? SqliteDatePart(string part, DateTime? value) => throw NotInMemory();

        /// <summary><c>substr(text, start)</c>. A negative start counts from the end, matching SQLite <c>right</c>.</summary>
        [DbFunction("SQLite", "SUBSTR")]
        public static string SqliteSubstr(string value, long? start) => throw NotInMemory();

        [DbFunction("MySql", "SQRT")]
        public static double? MySqlSqrt(double? value) => throw NotInMemory();

        [DbFunction("MySql", "ADDDATE")]
        public static DateTime? MySqlAddDate(DateTime? value, int? days) => throw NotInMemory();

        [DbFunction("MySql", "TIMESTAMP")]
        public static DateTime? MySqlTimestamp(DateTime? value, TimeSpan? time) => throw NotInMemory();

        [DbFunction("MySql", "ADDTIME")]
        public static TimeSpan? MySqlAddTime(TimeSpan? value, TimeSpan? time) => throw NotInMemory();

        [DbFunction("MySql", "SEC_TO_TIME")]
        public static TimeSpan? MySqlSecToTime(int? seconds) => throw NotInMemory();

        [DbFunction("MySql", "MAKETIME")]
        public static TimeSpan? MySqlMakeTime(int? hour, int? minute, int? second) => throw NotInMemory();

        [DbFunction("MySql", "DATE")]
        public static DateTime? MySqlDate(DateTime? value) => throw NotInMemory();

        [DbFunction("MySql", "DAYOFWEEK")]
        public static int? MySqlDayOfWeek(DateTime? value) => throw NotInMemory();

        [DbFunction("MySql", "DAYOFYEAR")]
        public static int? MySqlDayOfYear(DateTime? value) => throw NotInMemory();

        private static NotSupportedException NotInMemory() =>
            new("Expresso EF6 functions can only be used in LINQ to Entities queries.");
    }
}
