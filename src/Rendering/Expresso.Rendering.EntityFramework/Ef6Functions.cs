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

        /// <summary>SQL Server <c>POWER</c> of an <c>int</c> base. The result stays an integer.</summary>
        [DbFunction("SqlServer", "POWER")]
        public static int? SqlServerPower(int? value, double? exponent) => throw NotInMemory();

        /// <summary>SQL Server <c>POWER</c> of an <c>int</c> base and an <c>int</c> exponent.</summary>
        [DbFunction("SqlServer", "POWER")]
        public static int? SqlServerPowerInt(int? value, int? exponent) => throw NotInMemory();

        /// <summary>SQL Server <c>POWER</c> of a <c>tinyint</c> base.</summary>
        [DbFunction("SqlServer", "POWER")]
        public static int? SqlServerPowerByte(byte? value, double? exponent) => throw NotInMemory();

        /// <summary>SQL Server <c>POWER</c> of a <c>tinyint</c> base and an <c>int</c> exponent.</summary>
        [DbFunction("SqlServer", "POWER")]
        public static int? SqlServerPowerByteInt(byte? value, int? exponent) => throw NotInMemory();

        /// <summary>SQL Server <c>FLOOR</c> of an <c>int</c>. The result stays an integer.</summary>
        [DbFunction("SqlServer", "FLOOR")]
        public static int? SqlServerFloor(int? value) => throw NotInMemory();

        /// <summary>SQL Server <c>CEILING</c> of an <c>int</c>. The result stays an integer.</summary>
        [DbFunction("SqlServer", "CEILING")]
        public static int? SqlServerCeiling(int? value) => throw NotInMemory();

        /// <summary>SQL Server <c>ROUND</c> of an <c>int</c>. The result stays an integer.</summary>
        [DbFunction("SqlServer", "ROUND")]
        public static int? SqlServerRound(int? value, int? digits) => throw NotInMemory();

        /// <summary>Canonical <c>Abs</c> so a literal minimum <c>int</c> is not evaluated in the CLR.</summary>
        [DbFunction("Edm", "Abs")]
        public static int? EdmAbs(int? value) => throw NotInMemory();

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
