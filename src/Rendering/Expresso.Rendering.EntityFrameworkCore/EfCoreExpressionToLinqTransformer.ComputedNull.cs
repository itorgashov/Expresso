using System.Linq.Expressions;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.EntityFrameworkCore
{
    public partial class EfCoreExpressionToLinqTransformer
    {
        private static readonly string[] OracleEmptyStringFunctions =
            { "substring", "left", "right", "trim", "ltrim", "rtrim", "replace", "lower", "upper" };

        /// <inheritdoc />
        protected override Expression? ComputedNull(string function, IReadOnlyList<LinqNode> args, Expression value)
        {
            if (function == "sqrt")
            {
                return Provider is EfCoreProvider.Sqlite or EfCoreProvider.MySql
                    ? Expression.LessThan(args[0].Value, Expression.Constant(0.0))
                    : DomainNull(value);
            }

            if (function == "power" || (function == "abs" && value.Type == typeof(int)))
            {
                return DomainNull(value);
            }

            if (function is "div" or "mod")
            {
                return Provider is EfCoreProvider.Sqlite or EfCoreProvider.MySql
                    ? Expression.Equal(args[1].Value, Zero(args[1].Type))
                    : DomainNull(value);
            }

            if (Provider == EfCoreProvider.Oracle && function == "replace" && IsEmptyStringLiteral(args[0].Value))
            {
                return LinqEx.True;
            }

            if (Provider == EfCoreProvider.Oracle && function != "replace" && args.Any(a => IsEmptyStringLiteral(a.Value))
                && (function is "len" or "indexof" || OracleEmptyStringFunctions.Contains(function)))
            {
                return LinqEx.True;
            }

            if (Provider == EfCoreProvider.Oracle && OracleEmptyStringFunctions.Contains(function) && value.Type == typeof(string))
            {
                var method = ExpressoFunctionTranslations.Find(Provider, nameof(ExpressoDbFunctions.IsNullValue), new[] { typeof(string) });
                return method is null ? null : Expression.Equal(Expression.Call(method, value), Expression.Constant(true));
            }

            if (function == "substring" || function is "add" or "sub" or "mult")
            {
                return DomainNull(value);
            }

            if (Provider == EfCoreProvider.SqlServer && function is "left" or "right")
            {
                return DomainNull(value);
            }

            if (Provider == EfCoreProvider.SqlServer && function == "round" && value.Type == typeof(int))
            {
                return DomainNull(value);
            }

            return base.ComputedNull(function, args, value);
        }

        /// <inheritdoc />
        protected override Expression? NullFromArguments(string? function, IReadOnlyList<LinqNode> arguments) =>
            Provider == EfCoreProvider.Oracle && function == "replace"
                ? arguments[0].IsNull
                : base.NullFromArguments(function, arguments);

        /// <summary>Nullable SQL null-check that still contains <paramref name="value"/>.</summary>
        private Expression DomainNull(Expression value)
        {
            var method = ExpressoFunctionTranslations.Find(Provider, nameof(ExpressoDbFunctions.IsDomainNull), new[] { value.Type });
            return method is null ? ValueIsNull(value) : Expression.Call(method, value);
        }

        private static Expression Zero(Type type) =>
            Expression.Constant(Convert.ChangeType(0, type, System.Globalization.CultureInfo.InvariantCulture), type);

        /// <inheritdoc />
        protected override Expression Left(Expression source, Expression length)
        {
            var native = Marker(nameof(ExpressoDbFunctions.Left), source, length);
            if (native is not null)
            {
                return native;
            }

            return ReferencesQueryParameter(source) || ReferencesQueryParameter(length)
                ? base.Left(source, length)
                : LiteralCall(nameof(ExpressoDbFunctions.SqlSubstring), source, Expression.Constant(1), length) ?? base.Left(source, length);
        }

        /// <inheritdoc />
        protected override Expression Right(Expression source, Expression length) =>
            Marker(nameof(ExpressoDbFunctions.Right), source, length)
            ?? LiteralCall(nameof(ExpressoDbFunctions.SqlRight), source, length)
            ?? base.Right(source, length);
    }
}
