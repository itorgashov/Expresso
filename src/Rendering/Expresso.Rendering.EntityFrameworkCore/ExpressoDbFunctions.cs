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

        /// <summary><c>date</c> of a timestamp.</summary>
        public static DateOnly Date(DateTime value) => throw NotInMemory(nameof(Date));

        /// <summary><c>time</c> of a timestamp.</summary>
        public static TimeOnly Time(DateTime value) => throw NotInMemory(nameof(Time));

        /// <summary><c>round</c> to <paramref name="digits"/> decimal places.</summary>
        public static double Round(double value, int digits) => throw NotInMemory(nameof(Round));

        /// <summary>Zero-based <c>indexof</c>, <c>-1</c> when missing.</summary>
        public static int IndexOf(string source, string find) => throw NotInMemory(nameof(IndexOf));

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

        /// <summary><c>addhours</c> on a time of day.</summary>
        public static TimeSpan AddHours(TimeSpan value, int amount) => throw NotInMemory(nameof(AddHours));

        /// <summary><c>addminutes</c> on a time of day.</summary>
        public static TimeSpan AddMinutes(TimeSpan value, int amount) => throw NotInMemory(nameof(AddMinutes));

        /// <summary><c>addseconds</c> on a time of day.</summary>
        public static TimeSpan AddSeconds(TimeSpan value, int amount) => throw NotInMemory(nameof(AddSeconds));

        /// <summary><c>hour</c> of a time of day.</summary>
        public static int Hour(TimeSpan value) => throw NotInMemory(nameof(Hour));

        /// <summary><c>minute</c> of a time of day.</summary>
        public static int Minute(TimeSpan value) => throw NotInMemory(nameof(Minute));

        /// <summary><c>second</c> of a time of day.</summary>
        public static int Second(TimeSpan value) => throw NotInMemory(nameof(Second));

        private static InvalidOperationException NotInMemory(string name) =>
            new($"{nameof(ExpressoDbFunctions)}.{name} is translated to SQL by EF Core and cannot run in memory. Register it with HasExpressoFunctions.");
    }
}
