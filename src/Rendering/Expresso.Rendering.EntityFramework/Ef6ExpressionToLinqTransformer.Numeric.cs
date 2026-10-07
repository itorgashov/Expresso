using System.Linq.Expressions;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.EntityFramework
{
    public partial class Ef6ExpressionToLinqTransformer
    {
        private const string OracleIntegerPower = "integer-to-double promotion changes Oracle NUMBER arithmetic to BINARY_DOUBLE";

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

            var baseValue = LinqEx.ConvertTo(left, typeof(double));
            var exponentValue = LinqEx.ConvertTo(right, typeof(double));
            if (Provider == Ef6Provider.Oracle)
            {
                var promotion = new OracleIntegerPromotionProbe();
                promotion.Visit(baseValue);
                promotion.Visit(exponentValue);
                if (promotion.Found)
                {
                    throw Unsupported("power", OracleIntegerPower);
                }
            }

            var power = base.Power(baseValue, exponentValue);
            return Provider == Ef6Provider.Sqlite ? RoundPowerUnderflow(baseValue, exponentValue, power) : power;
        }

        /// <summary>
        /// System.Data.SQLite can return the smallest subnormal when the power rounds to zero.
        /// Evaluate the half exponent outside the subnormal range and let multiplication round the result;
        /// exact binary half-way ties need an explicit ties-to-even correction.
        /// </summary>
        private static Expression RoundPowerUnderflow(Expression baseValue, Expression exponent, Expression value)
        {
            var absolute = Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Abs), typeof(double)), value);
            var zero = Expression.Constant(0.0);
            var smallest = Expression.Constant(double.Epsilon);
            var baseMagnitude = Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Abs), typeof(double)), baseValue);
            // Both logarithms must run in SQL. A CLR-folded LOG(2) loses digits in EF6's SQL literal formatting.
            var logarithm = Function(nameof(Ef6Functions.SqliteLog));
            var log2 = Expression.Divide(Call(logarithm, baseMagnitude), Call(logarithm, Expression.Constant(2.0)));
            var binaryExponent = Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Round), typeof(double)), log2);
            var binaryBase = Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Pow), typeof(double), typeof(double)), Expression.Constant(2.0), binaryExponent);
            // A finite double power-of-two base has an exponent k in [-1074,1023]. The exact tie needs
            // exponent = -1075/k to be representable in binary: k's odd part must divide 1075.
            // 1075*1024 includes every possible power-of-two factor in that range, so this modulo tests it exactly.
            var dyadicTie = Expression.Equal(Expression.Modulo(Expression.Constant(1100800.0), binaryExponent), zero);
            var exactTie = Expression.AndAlso(
                Expression.Equal(baseMagnitude, binaryBase),
                Expression.AndAlso(dyadicTie, Expression.Equal(Expression.Multiply(exponent, binaryExponent), Expression.Constant(-1075.0))));
            var halfPower = Expression.Call(
                LinqEx.Method(typeof(Math), nameof(Math.Pow), typeof(double), typeof(double)),
                baseMagnitude,
                Expression.Divide(exponent, Expression.Constant(2.0)));
            var roundedZero = Expression.Equal(Expression.Multiply(halfPower, halfPower), zero);
            var roundsToZero = Expression.AndAlso(
                Expression.GreaterThan(absolute, zero),
                Expression.AndAlso(
                    Expression.LessThanOrEqual(absolute, smallest),
                    Expression.OrElse(exactTie, roundedZero)));
            return Expression.Condition(roundsToZero, zero, value);
        }

        private sealed class OracleIntegerPromotionProbe : ExpressionVisitor
        {
            public bool Found { get; private set; }

            protected override Expression VisitUnary(UnaryExpression node)
            {
                var operand = Visit(node.Operand);
                if (node.NodeType == ExpressionType.Convert && node.Type == typeof(double)
                    && (operand.Type == typeof(int) || operand.Type == typeof(byte)))
                {
                    Found = true;
                }

                return node.Update(operand);
            }
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
