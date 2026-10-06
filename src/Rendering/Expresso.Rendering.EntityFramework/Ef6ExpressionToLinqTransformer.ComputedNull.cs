using System.Data.Entity;
using System.Linq.Expressions;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.EntityFramework
{
    public partial class Ef6ExpressionToLinqTransformer
    {
        private static readonly string[] OracleEmptyStringFunctions =
            { "substring", "left", "right", "trim", "ltrim", "rtrim", "replace", "lower", "upper" };

        /// <inheritdoc />
        protected override Expression? ComputedNull(string function, IReadOnlyList<LinqNode> args, Expression value)
        {
            if (function == "sqrt")
            {
                return Provider is Ef6Provider.Sqlite or Ef6Provider.MySql
                    ? Expression.LessThan(args[0].Value, Expression.Constant(0.0))
                    : ValueIsNull(value);
            }

            if (function == "power" || (function == "abs" && value.Type == typeof(int)))
            {
                return ValueIsNull(value);
            }

            if (function is "div" or "mod")
            {
                return Provider is Ef6Provider.Sqlite or Ef6Provider.MySql
                    ? Expression.Equal(args[1].Value, ConstantZero(args[1].Type))
                    : ValueIsNull(value);
            }

            if (Provider == Ef6Provider.Oracle && args.Any(a => IsEmptyStringLiteral(a.Value))
                && function is "len" or "lower" or "upper" or "indexof")
            {
                return LinqEx.True;
            }

            if (Provider == Ef6Provider.Oracle && OracleEmptyStringFunctions.Contains(function) && value.Type == typeof(string))
            {
                return Expression.Equal(value, Expression.Constant(null, typeof(string)));
            }

            if (function == "substring" && Provider != Ef6Provider.Oracle)
            {
                return ValueIsNull(value);
            }

            if (function is "add" or "sub" or "mult")
            {
                return ValueIsNull(value);
            }

            if (Provider == Ef6Provider.SqlServer && function is "left" or "right")
            {
                return ValueIsNull(value);
            }

            if (Provider == Ef6Provider.SqlServer && function == "round" && value.Type == typeof(int))
            {
                return ValueIsNull(value);
            }

            return base.ComputedNull(function, args, value);
        }

        /// <inheritdoc />
        protected override Expression Right(Expression source, Expression length) => Provider switch
        {
            Ef6Provider.SqlServer => Expression.Call(DbFunction(nameof(DbFunctions.Right), typeof(string), typeof(long?)), source, Expression.Convert(length, typeof(long?))),
            Ef6Provider.PostgreSql => PostgreSqlRight(source, length),
            Ef6Provider.Sqlite => Call(Function(nameof(Ef6Functions.SqliteSubstr)), source, Expression.Convert(Expression.Negate(length), typeof(long))),
            _ => base.Right(source, length),
        };

        /// <inheritdoc />
        protected override Expression? NullFromArguments(string? function, IReadOnlyList<LinqNode> arguments) =>
            Provider == Ef6Provider.Oracle && function == "replace"
                ? arguments[0].IsNull
                : base.NullFromArguments(function, arguments);

        private static Expression ConstantZero(Type type) =>
            Expression.Constant(Convert.ChangeType(0, type, System.Globalization.CultureInfo.InvariantCulture), type);
    }
}
