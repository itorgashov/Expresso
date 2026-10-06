namespace Expresso.Rendering.EntityFrameworkCore
{
    /// <summary>
    /// Marker methods that <see cref="EfCoreExpressionToLinqTransformer"/> emits where a provider's native translation
    /// differs from the Expresso SQL renderer (or is missing). They translate to SQL only; calling them in memory throws.
    /// </summary>
    public static class ExpressoDbFunctions
    {
        /// <summary><c>dayofweek</c>, Sunday = 0.</summary>
        public static int DayOfWeek(DateTime value) => throw NotInMemory(nameof(DayOfWeek));

        /// <summary><c>dayofweek</c> of a <c>DateOnly</c>, Sunday = 0.</summary>
        public static int DayOfWeek(DateOnly value) => throw NotInMemory(nameof(DayOfWeek));

        /// <summary><c>dayofyear</c> of a <c>DateOnly</c>.</summary>
        public static int DayOfYear(DateOnly value) => throw NotInMemory(nameof(DayOfYear));

        /// <summary><c>year</c> of a <c>DateOnly</c>.</summary>
        public static int Year(DateOnly value) => throw NotInMemory(nameof(Year));

        /// <summary><c>month</c> of a <c>DateOnly</c>.</summary>
        public static int Month(DateOnly value) => throw NotInMemory(nameof(Month));

        /// <summary><c>day</c> of a <c>DateOnly</c>.</summary>
        public static int Day(DateOnly value) => throw NotInMemory(nameof(Day));

        /// <summary><c>date</c> of a timestamp.</summary>
        public static DateOnly Date(DateTime value) => throw NotInMemory(nameof(Date));

        /// <summary><c>time</c> of a timestamp.</summary>
        public static TimeOnly Time(DateTime value) => throw NotInMemory(nameof(Time));

        /// <summary><c>round</c> to <paramref name="digits"/> decimal places.</summary>
        public static double Round(double value, int digits) => throw NotInMemory(nameof(Round));

        /// <summary>Zero-based <c>indexof</c>, <c>-1</c> when missing.</summary>
        public static int IndexOf(string source, string find) => throw NotInMemory(nameof(IndexOf));

        /// <summary><c>addyears</c> on a <c>DateOnly</c>.</summary>
        public static DateOnly AddYears(DateOnly value, int amount) => throw NotInMemory(nameof(AddYears));

        /// <summary><c>addmonths</c> on a <c>DateOnly</c>.</summary>
        public static DateOnly AddMonths(DateOnly value, int amount) => throw NotInMemory(nameof(AddMonths));

        /// <summary><c>adddays</c> on a <c>DateOnly</c>.</summary>
        public static DateOnly AddDays(DateOnly value, int amount) => throw NotInMemory(nameof(AddDays));

        /// <summary><c>addyears</c>.</summary>
        public static DateTime AddYears(DateTime value, int amount) => throw NotInMemory(nameof(AddYears));

        /// <summary><c>addmonths</c>.</summary>
        public static DateTime AddMonths(DateTime value, int amount) => throw NotInMemory(nameof(AddMonths));

        /// <summary><c>adddays</c>.</summary>
        public static DateTime AddDays(DateTime value, int amount) => throw NotInMemory(nameof(AddDays));

        /// <summary><c>addhours</c>.</summary>
        public static DateTime AddHours(DateTime value, int amount) => throw NotInMemory(nameof(AddHours));

        /// <summary><c>addminutes</c>.</summary>
        public static DateTime AddMinutes(DateTime value, int amount) => throw NotInMemory(nameof(AddMinutes));

        /// <summary><c>addseconds</c>.</summary>
        public static DateTime AddSeconds(DateTime value, int amount) => throw NotInMemory(nameof(AddSeconds));

        /// <summary><c>addhours</c> on a <c>TimeOnly</c>.</summary>
        public static TimeOnly AddHours(TimeOnly value, int amount) => throw NotInMemory(nameof(AddHours));

        /// <summary><c>addminutes</c> on a <c>TimeOnly</c>.</summary>
        public static TimeOnly AddMinutes(TimeOnly value, int amount) => throw NotInMemory(nameof(AddMinutes));

        /// <summary><c>addseconds</c> on a <c>TimeOnly</c>.</summary>
        public static TimeOnly AddSeconds(TimeOnly value, int amount) => throw NotInMemory(nameof(AddSeconds));

        /// <summary><c>addhours</c> on a time of day.</summary>
        public static TimeSpan AddHours(TimeSpan value, int amount) => throw NotInMemory(nameof(AddHours));

        /// <summary><c>addminutes</c> on a time of day.</summary>
        public static TimeSpan AddMinutes(TimeSpan value, int amount) => throw NotInMemory(nameof(AddMinutes));

        /// <summary><c>addseconds</c> on a time of day.</summary>
        public static TimeSpan AddSeconds(TimeSpan value, int amount) => throw NotInMemory(nameof(AddSeconds));

        /// <summary><c>hour</c> of a <c>TimeOnly</c>.</summary>
        public static int Hour(TimeOnly value) => throw NotInMemory(nameof(Hour));

        /// <summary><c>minute</c> of a <c>TimeOnly</c>.</summary>
        public static int Minute(TimeOnly value) => throw NotInMemory(nameof(Minute));

        /// <summary><c>second</c> of a <c>TimeOnly</c>.</summary>
        public static int Second(TimeOnly value) => throw NotInMemory(nameof(Second));

        /// <summary><c>hour</c> of a time of day.</summary>
        public static int Hour(TimeSpan value) => throw NotInMemory(nameof(Hour));

        /// <summary><c>minute</c> of a time of day.</summary>
        public static int Minute(TimeSpan value) => throw NotInMemory(nameof(Minute));

        /// <summary><c>second</c> of a time of day.</summary>
        public static int Second(TimeSpan value) => throw NotInMemory(nameof(Second));

        /// <summary>True when Oracle treats <paramref name="value"/> as NULL (including empty string).</summary>
        public static bool IsNullValue(string value) => throw NotInMemory(nameof(IsNullValue));

        /// <summary>
        /// True when <paramref name="value"/> is NULL. The SQL keeps <paramref name="value"/> so a domain error is raised
        /// instead of folding <c>isnull</c> to false.
        /// </summary>
        public static bool IsDomainNull(double value) => throw NotInMemory(nameof(IsDomainNull));

        /// <inheritdoc cref="IsDomainNull(double)"/>
        public static bool IsDomainNull(int value) => throw NotInMemory(nameof(IsDomainNull));

        /// <summary><c>sqrt</c> kept in SQL when every operand is a literal.</summary>
        public static double Sqrt(double value) => throw NotInMemory(nameof(Sqrt));

        /// <summary><c>div</c> kept in SQL when every operand is a literal.</summary>
        public static double Divide(double left, double right) => throw NotInMemory(nameof(Divide));

        /// <inheritdoc cref="Divide(double, double)"/>
        public static int Divide(int left, int right) => throw NotInMemory(nameof(Divide));

        /// <summary><c>mod</c> kept in SQL when every operand is a literal.</summary>
        public static double Modulo(double left, double right) => throw NotInMemory(nameof(Modulo));

        /// <inheritdoc cref="Modulo(double, double)"/>
        public static int Modulo(int left, int right) => throw NotInMemory(nameof(Modulo));

        /// <summary>1-based <c>substring</c> kept in SQL when every operand is a literal. SQL clamps the length.</summary>
        public static string SqlSubstring(string source, int start, int length) => throw NotInMemory(nameof(SqlSubstring));

        /// <summary><c>replace</c> kept in SQL when every operand is a literal.</summary>
        public static string SqlReplace(string source, string oldValue, string newValue) => throw NotInMemory(nameof(SqlReplace));

        /// <summary><c>len</c> kept in SQL when the argument is a literal. The engine counts characters.</summary>
        public static int SqlLength(string value) => throw NotInMemory(nameof(SqlLength));

        /// <summary><c>lower</c> kept in SQL when the argument is a literal.</summary>
        public static string SqlLower(string value) => throw NotInMemory(nameof(SqlLower));

        /// <summary><c>trim</c> kept in SQL when the argument is a literal. SQLite removes spaces only.</summary>
        public static string SqlTrim(string value) => throw NotInMemory(nameof(SqlTrim));

        /// <summary>Zero-based <c>indexof</c> kept in SQL when every operand is a literal.</summary>
        public static int SqlIndexOf(string source, string find) => throw NotInMemory(nameof(SqlIndexOf));

        /// <summary><c>right</c> kept in SQL when every operand is a literal and the provider has no standing <c>RIGHT</c> marker.</summary>
        public static string SqlRight(string source, int length) => throw NotInMemory(nameof(SqlRight));

        /// <summary><c>upper</c> kept in SQL when the argument is a literal. The engine's case mapping applies.</summary>
        public static string SqlUpper(string value) => throw NotInMemory(nameof(SqlUpper));

        /// <summary><c>ltrim</c> kept in SQL when the argument is a literal. SQL removes spaces only.</summary>
        public static string SqlLTrim(string value) => throw NotInMemory(nameof(SqlLTrim));

        /// <summary><c>rtrim</c> kept in SQL when the argument is a literal. SQL removes spaces only.</summary>
        public static string SqlRTrim(string value) => throw NotInMemory(nameof(SqlRTrim));

        /// <summary><c>round</c> kept in SQL when every operand is a literal. The engine accepts precision outside 0–15.</summary>
        public static double SqlRound(double value, int digits) => throw NotInMemory(nameof(SqlRound));

        /// <summary><c>power</c> kept in SQL when every operand is a literal.</summary>
        public static double SqlPower(double left, double right) => throw NotInMemory(nameof(SqlPower));

        /// <summary><c>abs</c> kept in SQL when the argument is a literal, including the minimum <c>int</c>.</summary>
        public static int SqlAbs(int value) => throw NotInMemory(nameof(SqlAbs));

        /// <inheritdoc cref="SqlAbs(int)"/>
        public static double SqlAbs(double value) => throw NotInMemory(nameof(SqlAbs));

        /// <summary><c>left</c> with native SQL (PostgreSQL negative length).</summary>
        public static string Left(string source, int length) => throw NotInMemory(nameof(Left));

        /// <summary><c>right</c> with native SQL (SQL Server trailing spaces, PostgreSQL negative length).</summary>
        public static string Right(string source, int length) => throw NotInMemory(nameof(Right));

        private static InvalidOperationException NotInMemory(string name) =>
            new($"{nameof(ExpressoDbFunctions)}.{name} is translated to SQL by EF Core and cannot run in memory. Register it with HasExpressoFunctions.");
    }
}
