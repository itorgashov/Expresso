using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Translation = System.Func<System.Collections.Generic.IReadOnlyList<Microsoft.EntityFrameworkCore.Query.SqlExpressions.SqlExpression>, Microsoft.EntityFrameworkCore.Query.SqlExpressions.SqlExpression>;

namespace Expresso.Rendering.EntityFrameworkCore
{
    internal static partial class ExpressoFunctionTranslations
    {
        /// <summary>
        /// Translations for calls EF would otherwise evaluate in the CLR because every operand is a captured literal.
        /// The SQL keeps the engine's domain errors and clamping.
        /// </summary>
        private static IEnumerable<(string Marker, Type Value, Translation Translation)> LiteralEntries(string substringFunction, bool sqrt = true)
        {
            var entries = new List<(string Marker, Type Value, Translation Translation)>
            {
                (nameof(ExpressoDbFunctions.Divide), typeof(double), a => Arithmetic(ExpressionType.Divide, a[0], a[1])),
                (nameof(ExpressoDbFunctions.Divide), typeof(int), a => Arithmetic(ExpressionType.Divide, a[0], a[1])),
                (nameof(ExpressoDbFunctions.Modulo), typeof(double), a => Arithmetic(ExpressionType.Modulo, a[0], a[1])),
                (nameof(ExpressoDbFunctions.Modulo), typeof(int), a => Arithmetic(ExpressionType.Modulo, a[0], a[1])),
                (nameof(ExpressoDbFunctions.SqlSubstring), typeof(string), a => Function(substringFunction, typeof(string), a[0], a[1], a[2])),
            };
            if (sqrt)
            {
                entries.Insert(0, (nameof(ExpressoDbFunctions.Sqrt), typeof(double), a => Function("SQRT", typeof(double), a[0])));
            }

            return entries;
        }

        /// <summary>Literal string calls whose BCL versions disagree with the engine (code points, case, trim, empty search).</summary>
        private static IEnumerable<(string Marker, Type Value, Translation Translation)> StringLiteralEntries(
            string lengthFunction,
            Translation? indexOf = null,
            Translation? right = null)
        {
            var entries = new List<(string Marker, Type Value, Translation Translation)>
            {
                (nameof(ExpressoDbFunctions.SqlReplace), typeof(string), a => Function("REPLACE", typeof(string), a[0], a[1], a[2])),
                (nameof(ExpressoDbFunctions.SqlLength), typeof(string), a => Function(lengthFunction, typeof(int), a[0])),
                (nameof(ExpressoDbFunctions.SqlLower), typeof(string), a => Function("LOWER", typeof(string), a[0])),
                (nameof(ExpressoDbFunctions.SqlTrim), typeof(string), a => Function("TRIM", typeof(string), a[0])),
                (nameof(ExpressoDbFunctions.SqlUpper), typeof(string), a => Function("UPPER", typeof(string), a[0])),
                (nameof(ExpressoDbFunctions.SqlLTrim), typeof(string), a => Function("LTRIM", typeof(string), a[0])),
                (nameof(ExpressoDbFunctions.SqlRTrim), typeof(string), a => Function("RTRIM", typeof(string), a[0])),
            };
            if (indexOf is not null)
            {
                entries.Add((nameof(ExpressoDbFunctions.SqlIndexOf), typeof(string), indexOf));
            }

            if (right is not null)
            {
                entries.Add((nameof(ExpressoDbFunctions.SqlRight), typeof(string), right));
            }

            return entries;
        }

        /// <summary>Literal numeric calls whose BCL versions disagree with the engine (midpoint rounding, domain results, integer limits).</summary>
        private static IEnumerable<(string Marker, Type Value, Translation Translation)> NumericLiteralEntries(bool round)
        {
            var entries = new List<(string Marker, Type Value, Translation Translation)>
            {
                (nameof(ExpressoDbFunctions.SqlPower), typeof(double), a => Function("POWER", typeof(double), a[0], a[1])),
                (nameof(ExpressoDbFunctions.SqlAbs), typeof(double), a => Function("ABS", typeof(double), a[0])),
                (nameof(ExpressoDbFunctions.SqlAbs), typeof(int), a => Function("ABS", typeof(int), a[0])),
            };
            if (round)
            {
                entries.Add((nameof(ExpressoDbFunctions.SqlRound), typeof(double), a => Function("ROUND", typeof(double), a[0], a[1])));
            }

            return entries;
        }

        private static SqlExpression Negate(SqlExpression value) =>
            new SqlUnaryExpression(ExpressionType.Negate, value, value.Type, value.TypeMapping);

        private static SqlExpression MinusOne(SqlExpression value) =>
            Binary(ExpressionType.Subtract, value, Fragment("1"));

        private static SqlExpression Arithmetic(ExpressionType op, SqlExpression left, SqlExpression right) =>
            new SqlBinaryExpression(op, left, right, left.Type, left.TypeMapping);
    }
}
