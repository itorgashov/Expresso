using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Translation = System.Func<System.Collections.Generic.IReadOnlyList<Microsoft.EntityFrameworkCore.Query.SqlExpressions.SqlExpression>, Microsoft.EntityFrameworkCore.Query.SqlExpressions.SqlExpression>;

namespace Expresso.Rendering.EntityFrameworkCore
{
    /// <summary>
    /// SQL for each <see cref="ExpressoDbFunctions"/> marker, per provider. Each entry exists only because the provider's
    /// native translation is missing or differs from the dialect renderer (proven by the differential integration tests).
    /// Entries name the marker and the type of its first parameter (markers are overloaded for time-of-day values).
    /// </summary>
    internal static class ExpressoFunctionTranslations
    {
        private static readonly Type Timestamp = typeof(DateTime);
        private static readonly Type TimeOfDay = typeof(TimeSpan);

        private static readonly (string Name, string Unit, int Seconds)[] TimeUnits =
        {
            (nameof(ExpressoDbFunctions.AddHours), "HOUR", 3600),
            (nameof(ExpressoDbFunctions.AddMinutes), "MINUTE", 60),
            (nameof(ExpressoDbFunctions.AddSeconds), "SECOND", 1),
        };

        private static readonly (string Name, string Sqlite)[] TimeParts =
        {
            (nameof(ExpressoDbFunctions.Hour), "%H"),
            (nameof(ExpressoDbFunctions.Minute), "%M"),
            (nameof(ExpressoDbFunctions.Second), "%S"),
        };

        private static readonly IReadOnlyDictionary<EfCoreProvider, IReadOnlyDictionary<MethodInfo, Translation>> Map =
            new Dictionary<EfCoreProvider, IReadOnlyDictionary<MethodInfo, Translation>>
            {
                [EfCoreProvider.SqlServer] = Table(new (string, Type, Translation)[]
                {
                    (nameof(ExpressoDbFunctions.DayOfWeek), Timestamp, a => Binary(ExpressionType.Modulo,
                        Binary(ExpressionType.Subtract,
                            Binary(ExpressionType.Add, Function("DATEPART", typeof(int), Fragment("weekday"), a[0]), Fragment("@@DATEFIRST")),
                            Fragment("1")),
                        Fragment("7"))),
                    (nameof(ExpressoDbFunctions.Time), Timestamp, a => Cast(a[0], typeof(TimeOnly), new TimeOnlyTypeMapping("time"))),
                    (nameof(ExpressoDbFunctions.IndexOf), typeof(string), a => SqlServerIndexOf(a[0], a[1])),
                }.Concat(TimeUnits.Select(u => (u.Name, TimeOfDay, (Translation)(a =>
                    Function("DATEADD", TimeOfDay, Fragment(u.Unit.ToLowerInvariant()), a[1], a[0])))))),

                [EfCoreProvider.PostgreSql] = Table(new (string, Type, Translation)[]
                {
                    (nameof(ExpressoDbFunctions.Round), typeof(double), a =>
                        Function("ROUND", typeof(double), Cast(a[0], typeof(decimal), new DecimalTypeMapping("numeric")), a[1])),
                }),

                // DATE_ADD(time, INTERVAL n unit) = ADDTIME(time, SEC_TO_TIME(n * seconds)); neither wraps at 24 hours.
                [EfCoreProvider.MySql] = Table(new (string, Type, Translation)[]
                {
                    (nameof(ExpressoDbFunctions.Time), Timestamp, a => Cast(a[0], typeof(TimeOnly), new TimeOnlyTypeMapping("time"))),
                }.Concat(TimeUnits.Select(u => (u.Name, TimeOfDay, (Translation)(a => Function("ADDTIME", TimeOfDay, a[0],
                    Function("SEC_TO_TIME", TimeOfDay, MySqlTime,
                        new SqlBinaryExpression(ExpressionType.Multiply, a[1], Fragment(u.Seconds.ToString()), typeof(int), MySqlInt)))))))
                 .Concat(TimeParts.Select(p => (p.Name, TimeOfDay, (Translation)(a => Function(p.Name.ToUpperInvariant(), typeof(int), a[0])))))),

                [EfCoreProvider.Sqlite] = Table(new (string, Type, Translation)[]
                {
                    (nameof(ExpressoDbFunctions.Time), Timestamp, a => Function("time", typeof(TimeOnly), a[0])),
                }.Concat(TimeUnits.Select(u => (u.Name, TimeOfDay, (Translation)(a => Function("datetime", TimeOfDay, a[0],
                    Function("printf", typeof(string), SqliteText, Fragment($"'%d {u.Unit.ToLowerInvariant()}s'"), a[1]))))))
                 .Concat(TimeParts.Select(p => (p.Name, TimeOfDay, (Translation)(a =>
                    Cast(Function("strftime", typeof(string), SqliteText, Fragment($"'{p.Sqlite}'"), a[0]), typeof(int), new IntTypeMapping("INTEGER"))))))),

                // Oracle EF stores DateOnly/TimeOnly as ISO text, so date/time render the same text the parameter carries.
                [EfCoreProvider.Oracle] = Table(new (string, Type, Translation)[]
                {
                    (nameof(ExpressoDbFunctions.DayOfWeek), Timestamp, a => Binary(ExpressionType.Subtract,
                        Function("TO_NUMBER", typeof(int), OracleNumber, Function("TO_CHAR", typeof(string), OracleText, a[0], Fragment("'D'"))),
                        Fragment("1"))),
                    (nameof(ExpressoDbFunctions.Date), Timestamp, a => Function("TO_CHAR", typeof(DateOnly),
                        Function("TRUNC", typeof(DateTime), OracleDate, a[0]), Fragment("'YYYY-MM-DD'"))),
                    (nameof(ExpressoDbFunctions.Time), Timestamp, a => OracleTimeText(a[0])),
                    // The provider's CASE WHEN find IS NULL THEN 0 turns an empty find ('' is NULL) into 0; INSTR gives NULL.
                    (nameof(ExpressoDbFunctions.IndexOf), typeof(string), a => Binary(ExpressionType.Subtract, Function("INSTR", typeof(int), a[0], a[1]), Fragment("1"))),
                }),

                // Db2 has no ADD_HOURS(time); on a timestamp it equals the labeled duration (time + n HOURS) the dialect renders.
                [EfCoreProvider.Db2] = Table(new (string, Type, Translation)[]
                {
                    (nameof(ExpressoDbFunctions.DayOfWeek), Timestamp, a => Binary(ExpressionType.Subtract, Function("DAYOFWEEK", typeof(int), a[0]), Fragment("1"))),
                    (nameof(ExpressoDbFunctions.Date), Timestamp, a => Function("DATE", typeof(DateOnly), a[0])),
                    (nameof(ExpressoDbFunctions.Time), Timestamp, a => Function("TIME", typeof(TimeOnly), a[0])),
                    (nameof(ExpressoDbFunctions.Round), typeof(double), a => Function("ROUND", typeof(double), a[0], a[1])),
                    (nameof(ExpressoDbFunctions.IndexOf), typeof(string), a => Binary(ExpressionType.Subtract, Function("LOCATE", typeof(int), a[1], a[0]), Fragment("1"))),
                    (nameof(ExpressoDbFunctions.AddYears), Timestamp, a => Function("ADD_YEARS", typeof(DateTime), a[0], a[1])),
                    (nameof(ExpressoDbFunctions.AddMonths), Timestamp, a => Db2AddMonths(a[0], a[1])),
                    (nameof(ExpressoDbFunctions.AddDays), Timestamp, a => Function("ADD_DAYS", typeof(DateTime), a[0], a[1])),
                }.Concat(TimeUnits.Select(u => (u.Name, Timestamp, (Translation)(a => Function($"ADD_{u.Unit}S", typeof(DateTime), a[0], a[1])))))
                 .Concat(TimeUnits.Select(u => (u.Name, TimeOfDay, (Translation)(a => Function("TIME", TimeOfDay,
                    Function($"ADD_{u.Unit}S", typeof(DateTime), Db2Timestamp,
                        Function("TIMESTAMP", typeof(DateTime), Db2Timestamp, Fragment("'2000-01-01'"), a[0]), a[1]))))))
                 .Concat(TimeParts.Select(p => (p.Name, TimeOfDay, (Translation)(a => Function(p.Name.ToUpperInvariant(), typeof(int), a[0])))))),
            };

        /// <summary>Translations registered for <paramref name="provider"/>.</summary>
        public static IEnumerable<KeyValuePair<MethodInfo, Translation>> For(EfCoreProvider provider) =>
            Map.TryGetValue(provider, out var table) ? table : Enumerable.Empty<KeyValuePair<MethodInfo, Translation>>();

        /// <summary>
        /// The <see cref="ExpressoDbFunctions"/> marker <paramref name="name"/> taking <paramref name="argumentTypes"/>
        /// when <paramref name="provider"/> overrides it.
        /// </summary>
        public static MethodInfo? Find(EfCoreProvider provider, string name, IReadOnlyList<Type> argumentTypes) =>
            Map.TryGetValue(provider, out var table)
                ? table.Keys.FirstOrDefault(m => m.Name == name && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(argumentTypes))
                : null;

        /// <summary>
        /// Db2 labeled duration <c>x + n MONTHS</c> keeps the day (clamped to the month end); <c>ADD_MONTHS</c> moves a
        /// month-end day to the new month end, so the extra days are taken back.
        /// </summary>
        private static SqlExpression Db2AddMonths(SqlExpression value, SqlExpression amount)
        {
            var moved = Function("ADD_MONTHS", typeof(DateTime), value, amount);
            var excess = Binary(ExpressionType.Subtract, Function("DAY", typeof(int), value), Function("DAY", typeof(int), moved));
            return Function("ADD_DAYS", typeof(DateTime), moved, Function("LEAST", typeof(int), Fragment("0"), excess));
        }

        /// <summary>
        /// The dialect renderer's SQL Server <c>indexof</c>: <c>0</c> for an empty <c>find</c> (native <c>CHARINDEX</c> gives 0),
        /// tested with <c>DATALENGTH</c> because <c>= N''</c> ignores trailing spaces.
        /// </summary>
        private static SqlExpression SqlServerIndexOf(SqlExpression text, SqlExpression find)
        {
            var emptyFind = new SqlBinaryExpression(
                ExpressionType.AndAlso,
                new SqlBinaryExpression(ExpressionType.Equal, Function("DATALENGTH", typeof(int), SqlServerInt, find), Fragment("0"), typeof(bool), SqlServerBool),
                new SqlUnaryExpression(ExpressionType.NotEqual, text, typeof(bool), SqlServerBool),
                typeof(bool),
                SqlServerBool);
            return new CaseExpression(
                new[] { new CaseWhenClause(emptyFind, new SqlConstantExpression(Expression.Constant(0), SqlServerInt)) },
                new SqlBinaryExpression(ExpressionType.Subtract, Function("CHARINDEX", typeof(int), SqlServerInt, find, text), Fragment("1"), typeof(int), SqlServerInt));
        }

        // Nested function calls need an explicit type mapping (only the translation root gets one from the marker).
        private static readonly RelationalTypeMapping SqlServerInt = new IntTypeMapping("int");
        private static readonly RelationalTypeMapping SqlServerBool = new BoolTypeMapping("bit");
        private static readonly RelationalTypeMapping MySqlTime = new TimeSpanTypeMapping("time");
        private static readonly RelationalTypeMapping MySqlInt = new IntTypeMapping("int");
        private static readonly RelationalTypeMapping SqliteText = new StringTypeMapping("TEXT", System.Data.DbType.String);
        private static readonly RelationalTypeMapping Db2Timestamp = new DateTimeTypeMapping("TIMESTAMP", System.Data.DbType.DateTime);
        private static readonly RelationalTypeMapping OracleNumber = new IntTypeMapping("NUMBER(10)", System.Data.DbType.Int32);
        private static readonly RelationalTypeMapping OracleText = new StringTypeMapping("VARCHAR2(4000)", System.Data.DbType.String);
        private static readonly RelationalTypeMapping OracleDate = new DateTimeTypeMapping("DATE", System.Data.DbType.DateTime);
        private static readonly RelationalTypeMapping OracleBool = new BoolTypeMapping("NUMBER(1)", System.Data.DbType.Boolean);

        /// <summary>Oracle EF <c>TimeOnly</c> text: <c>HH:mm:ss</c>, plus seven fraction digits only when they are not all zero.</summary>
        private static SqlExpression OracleTimeText(SqlExpression value)
        {
            var wholeSecond = new SqlBinaryExpression(
                ExpressionType.Equal,
                Function("TO_CHAR", typeof(string), OracleText, value, Fragment("'FF7'")),
                Fragment("'0000000'"),
                typeof(bool),
                OracleBool);
            return new CaseExpression(
                new[] { new CaseWhenClause(wholeSecond, Function("TO_CHAR", typeof(TimeOnly), value, Fragment("'HH24:MI:SS'"))) },
                Function("TO_CHAR", typeof(TimeOnly), value, Fragment("'HH24:MI:SS.FF7'")));
        }

        private static IReadOnlyDictionary<MethodInfo, Translation> Table(IEnumerable<(string Marker, Type Value, Translation Translation)> entries) =>
            entries.ToDictionary(
                e => typeof(ExpressoDbFunctions).GetMethods().Single(m => m.Name == e.Marker && m.GetParameters()[0].ParameterType == e.Value),
                e => e.Translation);

        private static SqlExpression Function(string name, Type type, params SqlExpression[] arguments) =>
            Function(name, type, null, arguments);

        private static SqlExpression Function(string name, Type type, RelationalTypeMapping? typeMapping, params SqlExpression[] arguments) =>
            new SqlFunctionExpression(name, arguments, nullable: true, arguments.Select(a => a is not SqlFragmentExpression), type, typeMapping);

        private static SqlExpression Binary(ExpressionType op, SqlExpression left, SqlExpression right) =>
            new SqlBinaryExpression(op, left, right, typeof(int), null);

        private static SqlExpression Cast(SqlExpression operand, Type type, RelationalTypeMapping storeType) =>
            new SqlUnaryExpression(ExpressionType.Convert, operand, type, storeType);

        private static SqlExpression Fragment(string sql) => new SqlFragmentExpression(sql);
    }
}
