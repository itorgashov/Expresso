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
    public class EfCoreExpressionToLinqTransformer : QueryableExpressionToLinqTransformer
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
        protected override Expression DatePart(Expression value, string property) =>
            Marker(property, value) ?? base.DatePart(value, property);

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
        protected override Expression Round(Expression value, Expression? digits) =>
            Marker(nameof(ExpressoDbFunctions.Round), value, digits ?? Expression.Constant(0)) ?? base.Round(value, digits);

        /// <inheritdoc />
        protected override Expression IndexOf(Expression source, Expression find) =>
            Marker(nameof(ExpressoDbFunctions.IndexOf), source, find) ?? base.IndexOf(source, find);

        /// <inheritdoc />
        protected override Expression DateAdd(LinqDatePart part, Expression value, Expression amount) =>
            Marker("Add" + part + "s", value, amount) ?? base.DateAdd(part, value, amount);

        /// <summary>
        /// SQLite translates <c>Contains</c> to case-sensitive <c>instr</c>; the dialect renderer uses <c>LIKE</c>
        /// (ASCII case-insensitive), with the same escaping as here.
        /// </summary>
        protected override Expression Contains(Expression source, Expression pattern) =>
            Provider == EfCoreProvider.Sqlite
                ? Expression.Call(Like, Expression.Property(null, typeof(EF), nameof(EF.Functions)), source, LikeContainsPattern(pattern), Expression.Constant(LikeEscape))
                : base.Contains(source, pattern);

        /// <summary>
        /// SQL Server / PostgreSQL <c>CONCAT</c> and Oracle <c>||</c> treat NULL as an empty string; Oracle's result is
        /// still NULL when every argument is (<c>''</c> is NULL there).
        /// </summary>
        protected override LinqNode Concat(IReadOnlyList<LinqNode> arguments) => Provider switch
        {
            EfCoreProvider.SqlServer or EfCoreProvider.PostgreSql => ConcatNullAsEmpty(arguments),
            EfCoreProvider.Oracle => ConcatNullAsEmpty(arguments, nullWhenAllNull: true),
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

        /// <summary><c>'%' + pattern + '%'</c> with <c>\</c>, <c>%</c> and <c>_</c> escaped by <c>\</c>, in that order.</summary>
        private static Expression LikeContainsPattern(Expression pattern)
        {
            var escaped = new[] { LikeEscape, "%", "_" }.Aggregate(pattern, (text, special) =>
                (Expression)Expression.Call(text, StringReplace, Expression.Constant(special), Expression.Constant(LikeEscape + special)));
            var percent = Expression.Constant("%");
            return Expression.Call(StringConcat, Expression.Call(StringConcat, percent, escaped), percent);
        }

        /// <summary>Call to the marker <paramref name="name"/> when this provider overrides it for these argument types.</summary>
        private Expression? Marker(string name, params Expression[] arguments)
        {
            var method = ExpressoFunctionTranslations.Find(Provider, name, arguments.Select(a => a.Type).ToArray());
            return method is null ? null : Expression.Call(method, arguments);
        }
    }
}
