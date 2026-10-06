using System.Linq.Expressions;

namespace Expresso.Rendering.EntityFrameworkCore
{
    public partial class EfCoreExpressionToLinqTransformer
    {
        /// <inheritdoc />
        protected override Expression Sqrt(Expression value) =>
            LiteralCall(nameof(ExpressoDbFunctions.Sqrt), value) ?? base.Sqrt(value);

        /// <inheritdoc />
        protected override Expression Divide(Expression left, Expression right) =>
            LiteralCall(nameof(ExpressoDbFunctions.Divide), left, right) ?? base.Divide(left, right);

        /// <inheritdoc />
        protected override Expression Modulo(Expression left, Expression right) =>
            LiteralCall(nameof(ExpressoDbFunctions.Modulo), left, right) ?? base.Modulo(left, right);

        /// <inheritdoc />
        protected override Expression Substring(Expression source, Expression start, Expression length) =>
            LiteralCall(nameof(ExpressoDbFunctions.SqlSubstring), source, start, length) ?? base.Substring(source, start, length);

        /// <inheritdoc />
        protected override Expression Replace(Expression source, Expression oldValue, Expression newValue) =>
            LiteralCall(nameof(ExpressoDbFunctions.SqlReplace), source, oldValue, newValue) ?? base.Replace(source, oldValue, newValue);

        /// <inheritdoc />
        protected override Expression Length(Expression source) =>
            LiteralCall(nameof(ExpressoDbFunctions.SqlLength), source) ?? base.Length(source);

        /// <inheritdoc />
        protected override Expression Lower(Expression source) =>
            LiteralCall(nameof(ExpressoDbFunctions.SqlLower), source) ?? base.Lower(source);

        /// <inheritdoc />
        protected override Expression Trim(Expression source) =>
            LiteralCall(nameof(ExpressoDbFunctions.SqlTrim), source) ?? base.Trim(source);

        /// <inheritdoc />
        protected override Expression Upper(Expression source) =>
            LiteralCall(nameof(ExpressoDbFunctions.SqlUpper), source) ?? base.Upper(source);

        /// <inheritdoc />
        protected override Expression LTrim(Expression source) =>
            LiteralCall(nameof(ExpressoDbFunctions.SqlLTrim), source) ?? base.LTrim(source);

        /// <inheritdoc />
        protected override Expression RTrim(Expression source) =>
            LiteralCall(nameof(ExpressoDbFunctions.SqlRTrim), source) ?? base.RTrim(source);

        /// <inheritdoc />
        protected override Expression Power(Expression left, Expression right) =>
            Marker(nameof(ExpressoDbFunctions.SqlPower), left, right) ?? base.Power(left, right);

        /// <inheritdoc />
        protected override Expression Abs(Expression value) =>
            LiteralCall(nameof(ExpressoDbFunctions.SqlAbs), value) ?? base.Abs(value);

        /// <summary>Marker call when no operand references the query row; otherwise <see langword="null"/>.</summary>
        private Expression? LiteralCall(string name, params Expression[] arguments) =>
            arguments.Any(ReferencesQueryParameter) ? null : Marker(name, arguments);

        private static bool ReferencesQueryParameter(Expression expression)
        {
            var probe = new ParameterProbe();
            probe.Visit(expression);
            return probe.Found;
        }

        private sealed class ParameterProbe : ExpressionVisitor
        {
            public bool Found { get; private set; }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                Found = true;
                return node;
            }
        }
    }
}
