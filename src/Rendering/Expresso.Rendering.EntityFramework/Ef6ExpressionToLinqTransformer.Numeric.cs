using System.Linq.Expressions;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.EntityFramework
{
    public partial class Ef6ExpressionToLinqTransformer
    {
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

            return base.Power(LinqEx.ConvertTo(left, typeof(double)), LinqEx.ConvertTo(right, typeof(double)));
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
