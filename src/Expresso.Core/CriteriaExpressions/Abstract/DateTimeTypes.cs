namespace Expresso.Core.CriteriaExpressions.Abstract
{
    internal static class DateTimeTypes
    {
        public static readonly Type[] Calendar = BuildCalendarTypes();
        public static readonly Type[] Time = BuildTimeTypes();

        private static Type[] BuildCalendarTypes()
        {
            var types = new List<Type> { typeof(DateTime) };

#if NET6_0_OR_GREATER
            types.Add(typeof(DateOnly));
#endif

            return types.ToArray();
        }

        private static Type[] BuildTimeTypes()
        {
            var types = new List<Type> { typeof(DateTime), typeof(TimeSpan) };

#if NET6_0_OR_GREATER
            types.Add(typeof(TimeOnly));
#endif

            return types.ToArray();
        }
    }
}
