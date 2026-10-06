using System.Linq.Expressions;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.EntityFramework
{
    public partial class Ef6ExpressionToLinqTransformer
    {
        /// <inheritdoc />
        protected override void ValidateField(Expression body)
        {
            if (Provider is Ef6Provider.Oracle or Ef6Provider.Sqlite
                && (Nullable.GetUnderlyingType(body.Type) ?? body.Type) == typeof(TimeSpan))
            {
                throw Unsupported("time", NoTimeOfDay);
            }
        }

        /// <summary>
        /// SQL Server <c>POWER</c> keeps an integer base and an integer result.
        /// A later double consumer casts that result. Other providers convert both arguments to <c>double</c> first.
        /// </summary>
        protected override Expression Power(Expression left, Expression right)
        {
            if (Provider == Ef6Provider.SqlServer && (left.Type == typeof(int) || left.Type == typeof(byte)))
            {
                if (right.Type == typeof(int) || right.Type == typeof(byte))
                {
                    var integerExponent = right.Type == typeof(byte) ? LinqEx.ConvertTo(right, typeof(int)) : right;
                    var integerName = left.Type == typeof(int)
                        ? nameof(Ef6Functions.SqlServerPowerInt)
                        : nameof(Ef6Functions.SqlServerPowerByteInt);
                    return Call(Function(integerName), left, integerExponent);
                }

                var exponent = LinqEx.ConvertTo(right, typeof(double));
                var name = left.Type == typeof(int)
                    ? nameof(Ef6Functions.SqlServerPower)
                    : nameof(Ef6Functions.SqlServerPowerByte);
                return Call(Function(name), left, exponent);
            }

            var power = base.Power(LinqEx.ConvertTo(left, typeof(double)), LinqEx.ConvertTo(right, typeof(double)));
            return Provider == Ef6Provider.Sqlite ? FlushSubnormal(power) : power;
        }

        /// <summary>
        /// System.Data.SQLite returns <c>2^-1075</c> as a subnormal. Current SQLite returns 0, so a non-zero
        /// magnitude below the smallest normal double becomes 0.
        /// </summary>
        private static Expression FlushSubnormal(Expression value)
        {
            var absolute = Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Abs), typeof(double)), value);
            var zero = Expression.Constant(0.0);
            var leastNormal = Expression.Constant(2.2250738585072014E-308);
            var subnormal = Expression.AndAlso(
                Expression.GreaterThan(absolute, zero),
                Expression.LessThan(absolute, leastNormal));
            return Expression.Condition(subnormal, zero, value);
        }

        /// <inheritdoc />
        protected override Expression Round(Expression value, Expression? digits)
        {
            if (Provider == Ef6Provider.PostgreSql)
            {
                throw Unsupported("round", "EF6 cannot cast to numeric, and PostgreSQL rounds double precision half to even");
            }

            if (Provider == Ef6Provider.SqlServer && value.Type == typeof(int))
            {
                return Call(Function(nameof(Ef6Functions.SqlServerRound)), value, digits ?? Expression.Constant(0));
            }

            return base.Round(value, digits);
        }

        /// <summary>SQL Server <c>FLOOR</c> of an <c>int</c> stays <c>int</c>. Other inputs convert to <c>double</c>.</summary>
        protected override Expression Floor(Expression value) =>
            Provider == Ef6Provider.SqlServer && value.Type == typeof(int)
                ? Call(Function(nameof(Ef6Functions.SqlServerFloor)), value)
                : base.Floor(value);

        /// <summary>SQL Server <c>CEILING</c> of an <c>int</c> stays <c>int</c>. Other inputs convert to <c>double</c>.</summary>
        protected override Expression Ceiling(Expression value) =>
            Provider == Ef6Provider.SqlServer && value.Type == typeof(int)
                ? Call(Function(nameof(Ef6Functions.SqlServerCeiling)), value)
                : base.Ceiling(value);

        /// <summary>Canonical <c>Abs</c> for <c>int</c>, so <c>int.MinValue</c> is not evaluated with <c>Math.Abs</c>.</summary>
        protected override Expression Abs(Expression value) =>
            value.Type == typeof(int) ? Call(Function(nameof(Ef6Functions.EdmAbs)), value) : base.Abs(value);
    }
}
