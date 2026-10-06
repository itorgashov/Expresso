using System.Linq.Expressions;
using System.Reflection;
using Expresso.Rendering.Linq;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore
{
    /// <summary>
    /// Queryable profile for EF Core. Hooks where a provider's native translation differs from the Expresso SQL
    /// renderer call <see cref="ExpressoDbFunctions"/> markers instead; the model must register them with
    /// <see cref="ExpressoModelBuilderExtensions.HasExpressoFunctions"/>.
    /// </summary>
    public partial class EfCoreExpressionToLinqTransformer : QueryableExpressionToLinqTransformer
    {
        private const string LikeEscape = "\\";
        private static readonly MethodInfo Like = LinqEx.Method(typeof(DbFunctionsExtensions), nameof(DbFunctionsExtensions.Like),
            typeof(DbFunctions), typeof(string), typeof(string), typeof(string));
        private static readonly MethodInfo StringReplace = LinqEx.Method(typeof(string), nameof(string.Replace), typeof(string), typeof(string));
        private static readonly MethodInfo StringConcat = LinqEx.Method(typeof(string), nameof(string.Concat), typeof(string), typeof(string));

        /// <summary>Transformer for <c>DbContext.Database.ProviderName</c>.</summary>
        public EfCoreExpressionToLinqTransformer(string? providerName)
            : this(EfCoreProviders.Resolve(providerName))
        {
        }

        /// <summary>Transformer for <paramref name="provider"/>.</summary>
        public EfCoreExpressionToLinqTransformer(EfCoreProvider provider)
        {
            Provider = provider;
        }

        /// <summary>Provider whose overrides are applied.</summary>
        public EfCoreProvider Provider { get; }

        /// <inheritdoc />
        protected override Expression DatePart(Expression value, string property)
        {
            var marker = property == nameof(DateTime.DayOfYear) ? nameof(ExpressoDbFunctions.DayOfYear) : property;
            return Marker(marker, value) ?? base.DatePart(value, property);
        }

        /// <inheritdoc />
        protected override Expression DayOfWeek(Expression value) =>
            Marker(nameof(ExpressoDbFunctions.DayOfWeek), value) ?? base.DayOfWeek(value);

        /// <inheritdoc />
        protected override Expression Date(Expression value) =>
            Marker(nameof(ExpressoDbFunctions.Date), value) ?? base.Date(value);

        /// <inheritdoc />
        protected override Expression Time(Expression value) =>
            Marker(nameof(ExpressoDbFunctions.Time), value) ?? base.Time(value);

        /// <inheritdoc />
        protected override Expression Round(Expression value, Expression? digits)
        {
            var count = digits ?? Expression.Constant(0);
            return Marker(nameof(ExpressoDbFunctions.Round), value, count)
                ?? LiteralCall(nameof(ExpressoDbFunctions.SqlRound), value, count)
                ?? base.Round(value, digits);
        }

        /// <inheritdoc />
        protected override Expression IndexOf(Expression source, Expression find) =>
            Marker(nameof(ExpressoDbFunctions.IndexOf), source, find)
            ?? LiteralCall(nameof(ExpressoDbFunctions.SqlIndexOf), source, find)
            ?? base.IndexOf(source, find);

        /// <inheritdoc />
        protected override Expression DateAdd(LinqDatePart part, Expression value, Expression amount) =>
            Marker("Add" + part + "s", value, amount) ?? base.DateAdd(part, value, amount);

        /// <inheritdoc />
        protected override Expression StartsWith(Expression source, Expression pattern) =>
            LiteralLike(source, pattern, prefix: false, suffix: true) ?? base.StartsWith(source, pattern);

        /// <inheritdoc />
        protected override Expression EndsWith(Expression source, Expression pattern) =>
            LiteralLike(source, pattern, prefix: true, suffix: false) ?? base.EndsWith(source, pattern);

        /// <summary>
        /// SQLite translates <c>Contains</c> to case-sensitive <c>instr</c>; the dialect renderer uses <c>LIKE</c>
        /// (ASCII case-insensitive), with the same escaping as here. A literal pair on any provider is also <c>LIKE</c>,
        /// because the CLR method would decide case before the collation can.
        /// </summary>
        protected override Expression Contains(Expression source, Expression pattern) =>
            Provider == EfCoreProvider.Sqlite || LiteralOperands(source, pattern)
                ? LikeCall(source, LikePattern(pattern, prefix: true, suffix: true))
                : base.Contains(source, pattern);

        /// <summary>
        /// SQL Server / PostgreSQL <c>CONCAT</c> and Oracle <c>||</c> treat NULL as an empty string; Oracle's result is
        /// still NULL when every argument is (<c>''</c> is NULL there).
        /// </summary>
        protected override LinqNode Concat(IReadOnlyList<LinqNode> arguments) => Provider switch
        {
            EfCoreProvider.SqlServer or EfCoreProvider.PostgreSql => ConcatNullAsEmpty(arguments),
            EfCoreProvider.Oracle => OracleConcat(arguments),
            _ => base.Concat(arguments),
        };

        /// <summary>SQL Server and DB2 <c>AVG</c> over an integer is an integer (truncated toward zero).</summary>
        protected override Expression Average(Expression items, LambdaExpression selector)
        {
            var average = base.Average(items, selector);
            return Provider is EfCoreProvider.SqlServer or EfCoreProvider.Db2 && selector.ReturnType == typeof(int?)
                ? Expression.Convert(average, typeof(int?))
                : average;
        }

        /// <summary><c>LIKE</c> when neither operand references the query row; otherwise <see langword="null"/>.</summary>
        private Expression? LiteralLike(Expression source, Expression pattern, bool prefix, bool suffix) =>
            LiteralOperands(source, pattern) ? LikeCall(source, LikePattern(pattern, prefix, suffix)) : null;

        private static bool LiteralOperands(params Expression[] arguments) => !arguments.Any(ReferencesQueryParameter);

        private static Expression LikeCall(Expression source, Expression pattern) =>
            Expression.Call(Like, Expression.Property(null, typeof(EF), nameof(EF.Functions)), source, pattern, Expression.Constant(LikeEscape));

        /// <summary>Escapes <c>\</c>, <c>%</c> and <c>_</c> with <c>\</c>, in that order, then adds the <c>%</c> wildcards.</summary>
        private static Expression LikePattern(Expression pattern, bool prefix, bool suffix)
        {
            var escaped = new[] { LikeEscape, "%", "_" }.Aggregate(pattern, (text, special) =>
                (Expression)Expression.Call(text, StringReplace, Expression.Constant(special), Expression.Constant(LikeEscape + special)));
            var percent = Expression.Constant("%");
            if (prefix)
            {
                escaped = Expression.Call(StringConcat, percent, escaped);
            }

            if (suffix)
            {
                escaped = Expression.Call(StringConcat, escaped, percent);
            }

            return escaped;
        }

        /// <summary>Call to the marker <paramref name="name"/> when this provider overrides it for these argument types.</summary>
        private Expression? Marker(string name, params Expression[] arguments)
        {
            var method = ExpressoFunctionTranslations.Find(Provider, name, arguments.Select(a => a.Type).ToArray());
            return method is null ? null : Expression.Call(method, arguments);
        }
    }
}
