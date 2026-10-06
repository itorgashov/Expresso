using System.Data.Entity;
using System.Linq.Expressions;
using System.Reflection;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.EntityFramework
{
    /// <summary>
    /// Queryable profile for Entity Framework 6. Hooks EF6 cannot translate, or translates differently from the Expresso
    /// SQL renderer, use canonical <see cref="DbFunctions"/> or, where the provider lacks them, provider store functions.
    /// Functions a provider cannot render exactly throw <see cref="NotSupportedException"/> instead of approximating.
    /// </summary>
    public partial class Ef6ExpressionToLinqTransformer : QueryableExpressionToLinqTransformer
    {
        private const string NoTimeOfDay = "the provider has no time-of-day (Edm.Time) type";
        private static readonly Expression KnownSunday = Expression.Constant(new DateTime(1900, 1, 7), typeof(DateTime?));
        private static readonly Expression Seven = Expression.Constant(7);
        private static readonly Expression One = Expression.Constant(1);
        private static readonly Expression Zero = Expression.Constant(0);

        /// <summary>Transformer for an ADO.NET provider invariant name (for example <c>System.Data.SqlClient</c>).</summary>
        public Ef6ExpressionToLinqTransformer(string? providerInvariantName)
            : this(Ef6Providers.Resolve(providerInvariantName))
        {
        }

        /// <summary>Transformer for the provider of <paramref name="context"/>'s connection.</summary>
        public Ef6ExpressionToLinqTransformer(DbContext context)
            : this(Ef6Providers.Resolve((context ?? throw new ArgumentNullException(nameof(context))).Database.Connection))
        {
        }

        /// <summary>Transformer for <paramref name="provider"/>.</summary>
        public Ef6ExpressionToLinqTransformer(Ef6Provider provider)
        {
            Provider = provider;
        }

        /// <summary>Provider whose overrides are applied.</summary>
        public Ef6Provider Provider { get; }

        /// <inheritdoc />
        protected override Expression DatePart(Expression value, string property) =>
            property != nameof(DateTime.DayOfYear) ? base.DatePart(value, property) : Provider switch
            {
                Ef6Provider.MySql => Call(Function(nameof(Ef6Functions.MySqlDayOfYear)), value),
                Ef6Provider.Sqlite => Call(Function(nameof(Ef6Functions.SqliteDatePart)), Expression.Constant("dayofyear"), value),
                Ef6Provider.SqlServer or Ef6Provider.Oracle => Call(Function(nameof(Ef6Functions.DayOfYear)), value),
                _ => Expression.Add(DiffDays(StartOfYear(value), value), One),
            };

        /// <summary><c>dayofweek</c>, Sunday = 0: days since a known Sunday, modulo 7.</summary>
        protected override Expression DayOfWeek(Expression value)
        {
            switch (Provider)
            {
                case Ef6Provider.MySql:
                    return Expression.Subtract(Call(Function(nameof(Ef6Functions.MySqlDayOfWeek)), value), One);
                case Ef6Provider.Sqlite:
                    return Call(Function(nameof(Ef6Functions.SqliteDatePart)), Expression.Constant("weekday"), value);
                default:
                    var days = DiffDays(KnownSunday, value);
                    return Expression.Modulo(Expression.Add(Expression.Modulo(days, Seven), Seven), Seven);
            }
        }

        /// <summary><c>DbFunctions.TruncateTime</c>.</summary>
        protected override Expression Date(Expression value) => Provider switch
        {
            Ef6Provider.MySql => Call(Function(nameof(Ef6Functions.MySqlDate)), value),
            Ef6Provider.Sqlite => throw Unsupported("date", "the provider does not translate TruncateTime"),
            _ => Call(DbFunction(nameof(DbFunctions.TruncateTime), typeof(DateTime?)), value),
        };

        /// <summary>Time of day of a <c>DateTime</c>.</summary>
        protected override Expression Time(Expression value)
        {
            Expression Part(string name) => Expression.Property(value, name);
            switch (Provider)
            {
                case Ef6Provider.Oracle or Ef6Provider.Sqlite:
                    throw Unsupported("time", NoTimeOfDay);
                case Ef6Provider.MySql:
                    return Call(Function(nameof(Ef6Functions.MySqlMakeTime)), Part(nameof(DateTime.Hour)), Part(nameof(DateTime.Minute)), Part(nameof(DateTime.Second)));
                case Ef6Provider.PostgreSql:
                    var sinceMidnight = Call(DbFunction(nameof(DbFunctions.DiffMilliseconds), typeof(DateTime?), typeof(DateTime?)), Date(value), value);
                    return Call(DbFunction(nameof(DbFunctions.AddMilliseconds), typeof(TimeSpan?), typeof(int?)), Expression.Constant(TimeSpan.Zero), sinceMidnight);
                default:
                    var seconds = Expression.Add(
                        Expression.Convert(Part(nameof(DateTime.Second)), typeof(double)),
                        Expression.Divide(Expression.Convert(Part(nameof(DateTime.Millisecond)), typeof(double)), Expression.Constant(1000.0)));
                    return Call(
                        DbFunction(nameof(DbFunctions.CreateTime), typeof(int?), typeof(int?), typeof(double?)),
                        Part(nameof(DateTime.Hour)),
                        Part(nameof(DateTime.Minute)),
                        seconds);
            }
        }

        /// <summary><c>DbFunctions.AddYears</c> … <c>AddSeconds</c> on a <c>DateTime</c> or time-of-day <c>TimeSpan</c>.</summary>
        protected override Expression DateAdd(LinqDatePart part, Expression value, Expression amount) => Provider switch
        {
            Ef6Provider.MySql => MySqlDateAdd(part, value, amount),
            Ef6Provider.Oracle => throw Unsupported(AddName(part), "the provider pastes the amount into an INTERVAL literal, so parameters fail (ORA-01867)"),
            Ef6Provider.Sqlite => throw Unsupported(AddName(part), "the provider translates no canonical date arithmetic"),
            _ => Call(DbFunction("Add" + part + "s", LinqEx.NullableType(value.Type), typeof(int?)), value, amount),
        };

        /// <inheritdoc />
        protected override Expression Left(Expression source, Expression length) => Provider switch
        {
            Ef6Provider.SqlServer => Expression.Call(DbFunction(nameof(DbFunctions.Left), typeof(string), typeof(long?)), source, Expression.Convert(length, typeof(long?))),
            Ef6Provider.PostgreSql => PostgreSqlLeft(source, length),
            _ => base.Left(source, length),
        };

        /// <summary>
        /// Npgsql EF6 renders <c>DbFunctions.Left/Right</c> as <c>substr</c> that rejects a negative length.
        /// A non-negative length keeps the usual slice; a negative length drops characters from the other end.
        /// </summary>
        private static Expression PostgreSqlLeft(Expression source, Expression length)
        {
            var count = Expression.Property(source, nameof(string.Length));
            var zero = Expression.Constant(0);
            var take = Expression.Condition(
                Expression.GreaterThanOrEqual(length, zero),
                Expression.Condition(Expression.LessThan(count, length), count, length),
                Expression.Condition(Expression.LessThanOrEqual(Expression.Add(count, length), zero), zero, Expression.Add(count, length)));
            return Expression.Call(source, LinqEx.Method(typeof(string), nameof(string.Substring), typeof(int), typeof(int)), zero, take);
        }

        /// <inheritdoc cref="PostgreSqlLeft" />
        private static Expression PostgreSqlRight(Expression source, Expression length)
        {
            var count = Expression.Property(source, nameof(string.Length));
            var zero = Expression.Constant(0);
            var substring = LinqEx.Method(typeof(string), nameof(string.Substring), typeof(int), typeof(int));
            var positive = Expression.Condition(
                Expression.LessThanOrEqual(count, length),
                source,
                Expression.Call(source, substring, Expression.Subtract(count, length), length));
            var take = Expression.Add(count, length);
            var negative = Expression.Condition(
                Expression.LessThanOrEqual(take, zero),
                Expression.Constant(string.Empty),
                Expression.Call(source, substring, Expression.Subtract(zero, length), take));
            return Expression.Condition(Expression.GreaterThanOrEqual(length, zero), positive, negative);
        }

        /// <inheritdoc />
        protected override Expression Round(Expression value, Expression? digits) =>
            Provider == Ef6Provider.PostgreSql
                ? throw Unsupported("round", "EF6 cannot cast to numeric, and PostgreSQL rounds double precision half to even")
                : base.Round(value, digits);

        /// <summary><c>CASE WHEN x &gt; 0 THEN 1 WHEN x &lt; 0 THEN -1 ELSE 0 END</c> (no canonical <c>SIGN</c>).</summary>
        protected override Expression Sign(Expression value)
        {
            var zero = Expression.Constant(Convert.ChangeType(0, value.Type), value.Type);
            return Expression.Condition(
                Expression.GreaterThan(value, zero),
                One,
                Expression.Condition(Expression.LessThan(value, zero), Expression.Constant(-1), Expression.Constant(0)));
        }

        /// <summary>Store <c>SQRT</c> (no canonical square root).</summary>
        protected override Expression Sqrt(Expression value) => Provider switch
        {
            Ef6Provider.SqlServer => Call(Function(nameof(Ef6Functions.SqlServerSqrt)), value),
            Ef6Provider.MySql => Call(Function(nameof(Ef6Functions.MySqlSqrt)), value),
            Ef6Provider.Sqlite => Call(Function(nameof(Ef6Functions.SqliteSqrt)), value),
            Ef6Provider.PostgreSql => throw Unsupported("sqrt", "the provider exposes no store functions"),
            Ef6Provider.Oracle => throw Unsupported("sqrt", "the provider manifest has no SQRT"),
            _ => base.Sqrt(value),
        };

        /// <summary>
        /// SQL Server and SQLite translate <c>IndexOf</c> to <c>CHARINDEX</c>, which gives -1 for an empty <c>find</c>; the
        /// dialect renderer gives 0. SQL Server tests emptiness with store <c>DATALENGTH</c> (<c>LEN</c> ignores trailing spaces).
        /// </summary>
        protected override Expression IndexOf(Expression source, Expression find) => Provider switch
        {
            Ef6Provider.SqlServer => EmptyFindIsZero(source, find, Call(Function(nameof(Ef6Functions.SqlServerDataLength)), find)),
            Ef6Provider.Sqlite => EmptyFindIsZero(source, find, Expression.Property(find, nameof(string.Length))),
            _ => base.IndexOf(source, find),
        };

        /// <inheritdoc cref="SqliteLike" />
        protected override Expression StartsWith(Expression source, Expression pattern) =>
            Provider == Ef6Provider.Sqlite ? SqliteLike(source, pattern, base.StartsWith) : base.StartsWith(source, pattern);

        /// <inheritdoc cref="SqliteLike" />
        protected override Expression EndsWith(Expression source, Expression pattern) =>
            Provider == Ef6Provider.Sqlite ? SqliteLike(source, pattern, base.EndsWith) : base.EndsWith(source, pattern);

        /// <inheritdoc cref="SqliteLike" />
        protected override Expression Contains(Expression source, Expression pattern) =>
            Provider == Ef6Provider.Sqlite ? SqliteLike(source, pattern, base.Contains) : base.Contains(source, pattern);

        /// <summary>
        /// SQL Server / PostgreSQL <c>CONCAT</c> and Oracle <c>||</c> treat NULL as an empty string; Oracle's result is
        /// still NULL when every argument is (<c>''</c> is NULL there).
        /// </summary>
        protected override LinqNode Concat(IReadOnlyList<LinqNode> arguments) => Provider switch
        {
            Ef6Provider.SqlServer or Ef6Provider.PostgreSql => ConcatNullAsEmpty(arguments),
            Ef6Provider.Oracle => OracleConcat(arguments),
            _ => base.Concat(arguments),
        };

        /// <summary>SQL Server <c>AVG</c> over an integer is an integer (truncated toward zero).</summary>
        protected override Expression Average(Expression items, LambdaExpression selector)
        {
            var average = base.Average(items, selector);
            return Provider == Ef6Provider.SqlServer && selector.ReturnType == typeof(int?)
                ? Expression.Convert(average, typeof(int?))
                : average;
        }

        /// <summary>MySql.Data has no canonical date arithmetic: <c>ADDDATE</c> for days, <c>SEC_TO_TIME</c> for clock units.</summary>
        private Expression MySqlDateAdd(LinqDatePart part, Expression value, Expression amount)
        {
            if (part == LinqDatePart.Day && value.Type == typeof(DateTime))
            {
                return Call(Function(nameof(Ef6Functions.MySqlAddDate)), value, amount);
            }

            var secondsPerUnit = part switch
            {
                LinqDatePart.Hour => 3600,
                LinqDatePart.Minute => 60,
                LinqDatePart.Second => 1,
                _ => throw Unsupported(AddName(part), "MySQL adds months only with INTERVAL syntax, which no store function can express"),
            };

            var duration = Call(Function(nameof(Ef6Functions.MySqlSecToTime)), Expression.Multiply(amount, Expression.Constant(secondsPerUnit)));
            return value.Type == typeof(TimeSpan)
                ? Call(Function(nameof(Ef6Functions.MySqlAddTime)), value, duration)
                : Call(Function(nameof(Ef6Functions.MySqlTimestamp)), value, duration);
        }

        /// <summary><c>0</c> when <paramref name="findLength"/> is 0 and <paramref name="source"/> is not NULL, otherwise the canonical <c>IndexOf</c>.</summary>
        private Expression EmptyFindIsZero(Expression source, Expression find, Expression findLength) =>
            Expression.Condition(
                Expression.AndAlso(
                    Expression.Equal(findLength, Zero),
                    Expression.NotEqual(source, Expression.Constant(null, source.Type))),
                Zero,
                base.IndexOf(source, find));

        /// <summary>
        /// SQLite translates <c>StartsWith</c> / <c>EndsWith</c> / <c>Contains</c> to case-sensitive <c>CHARINDEX</c>, which
        /// misses an empty pattern, and cannot escape <c>LIKE</c>. The dialect renderer's <c>LIKE</c> folds ASCII case, as
        /// SQLite <c>LOWER</c> does, and matches an empty pattern.
        /// </summary>
        private Expression SqliteLike(Expression source, Expression pattern, Func<Expression, Expression, Expression> test) =>
            Expression.OrElse(
                Expression.Equal(Expression.Property(pattern, nameof(string.Length)), Zero),
                test(Lower(source), Lower(pattern)));

        private NotSupportedException Unsupported(string function, string reason) =>
            new($"'{function}' cannot be rendered exactly on the EF6 {Provider} provider: {reason}.");

        private static string AddName(LinqDatePart part) => "add" + part.ToString().ToLowerInvariant() + "s";

        /// <summary>First day of <paramref name="value"/>'s year (same time of day).</summary>
        private static Expression StartOfYear(Expression value)
        {
            var addDays = DbFunction(nameof(DbFunctions.AddDays), typeof(DateTime?), typeof(int?));
            var addMonths = DbFunction(nameof(DbFunctions.AddMonths), typeof(DateTime?), typeof(int?));
            var firstOfMonth = Call(addDays, value, Expression.Subtract(One, Expression.Property(value, nameof(DateTime.Day))));
            return Call(addMonths, firstOfMonth, Expression.Subtract(One, Expression.Property(value, nameof(DateTime.Month))));
        }

        private static Expression DiffDays(Expression from, Expression to) =>
            Call(DbFunction(nameof(DbFunctions.DiffDays), typeof(DateTime?), typeof(DateTime?)), from, to);

        private static MethodInfo DbFunction(string name, params Type[] parameterTypes) =>
            LinqEx.Method(typeof(DbFunctions), name, parameterTypes);

        private static MethodInfo Function(string name) => typeof(Ef6Functions).GetMethod(name)!;

        /// <summary>Calls a nullable-in/nullable-out function on non-nullable arguments; the result is non-nullable.</summary>
        private static Expression Call(MethodInfo method, params Expression[] arguments)
        {
            var call = Expression.Call(method, arguments.Select(AsNullable));
            return Expression.Convert(call, Nullable.GetUnderlyingType(call.Type) ?? call.Type);
        }

        private static Expression AsNullable(Expression value) => LinqEx.ConvertTo(value, LinqEx.NullableType(value.Type));
    }
}
