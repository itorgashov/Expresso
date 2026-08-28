namespace Expresso.Core.CriteriaExpressions.Abstract
{
    internal static class SupportedOperandTypes
    {
        public static readonly Type[] Equality = BuildEqualityTypes();
        public static readonly Type[] Ordered = BuildOrderedTypes();
        public static readonly Type[] IsNull = BuildIsNullTypes();

        private static Type[] BuildEqualityTypes()
        {
            var types = new List<Type>
            {
                typeof(byte),
                typeof(int),
                typeof(double),
                typeof(DateTime),
                typeof(bool),
                typeof(string),
                typeof(Guid),
            };

#if NET6_0_OR_GREATER
            types.Add(typeof(DateOnly));
            types.Add(typeof(TimeOnly));
#endif

            return types.ToArray();
        }

        private static Type[] BuildOrderedTypes()
        {
            var types = new List<Type>
            {
                typeof(byte),
                typeof(int),
                typeof(double),
                typeof(DateTime),
            };

#if NET6_0_OR_GREATER
            types.Add(typeof(DateOnly));
            types.Add(typeof(TimeOnly));
#endif

            return types.ToArray();
        }

        private static Type[] BuildIsNullTypes()
        {
            var types = new List<Type>
            {
                typeof(bool),
                typeof(string),
                typeof(byte),
                typeof(int),
                typeof(double),
                typeof(DateTime),
                typeof(Guid),
            };

#if NET6_0_OR_GREATER
            types.Add(typeof(DateOnly));
            types.Add(typeof(TimeOnly));
#endif

            return types.ToArray();
        }
    }
}
