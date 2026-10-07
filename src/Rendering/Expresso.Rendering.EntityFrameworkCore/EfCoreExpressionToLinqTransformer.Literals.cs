using System.Linq.Expressions;
using Expresso.Core.CriteriaExpressions;
using Expresso.Rendering.Linq;

namespace Expresso.Rendering.EntityFrameworkCore
{
    public partial class EfCoreExpressionToLinqTransformer
    {
        /// <summary>Oracle numeric binds match the ADO NUMBER parameters before any arithmetic is evaluated.</summary>
        protected override LinqNode Literal(Literal literal)
        {
            var node = base.Literal(literal);
            return Provider == EfCoreProvider.Oracle && literal.Value is double
                ? LinqNode.Scalar(Marker(nameof(ExpressoDbFunctions.OracleNumber), node.Value)!, node.IsNull)
                : node;
        }

        /// <inheritdoc />
        protected override Expression Sqrt(Expression value) =>
            LiteralCall(nameof(ExpressoDbFunctions.Sqrt), value) ?? base.Sqrt(value);

        /// <inheritdoc />
        protected override Expression Divide(Expression left, Expression right) =>
            LiteralCall(nameof(ExpressoDbFunctions.Divide), OracleNumericOperand(left), OracleNumericOperand(right))
                ?? base.Divide(OracleNumericOperand(left), OracleNumericOperand(right));

        /// <inheritdoc />
        protected override Expression Modulo(Expression left, Expression right) =>
            LiteralCall(nameof(ExpressoDbFunctions.Modulo), OracleNumericOperand(left), OracleNumericOperand(right))
                ?? base.Modulo(OracleNumericOperand(left), OracleNumericOperand(right));

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
        protected override Expression Add(Expression left, Expression right) =>
            LiteralCall(nameof(ExpressoDbFunctions.SqlAdd), OracleNumericOperand(left), OracleNumericOperand(right))
                ?? base.Add(OracleNumericOperand(left), OracleNumericOperand(right));

        /// <inheritdoc />
        protected override Expression Subtract(Expression left, Expression right) =>
            LiteralCall(nameof(ExpressoDbFunctions.SqlSubtract), OracleNumericOperand(left), OracleNumericOperand(right))
                ?? base.Subtract(OracleNumericOperand(left), OracleNumericOperand(right));

        /// <inheritdoc />
        protected override Expression Multiply(Expression left, Expression right) =>
            LiteralCall(nameof(ExpressoDbFunctions.SqlMultiply), OracleNumericOperand(left), OracleNumericOperand(right))
                ?? base.Multiply(OracleNumericOperand(left), OracleNumericOperand(right));

        /// <inheritdoc />
        protected override Expression Power(Expression left, Expression right)
        {
            if (Provider == EfCoreProvider.SqlServer && (left.Type == typeof(int) || left.Type == typeof(byte)))
            {
                if (right.Type == typeof(int) || right.Type == typeof(byte))
                {
                    var integerExponent = right.Type == typeof(byte) ? LinqEx.ConvertTo(right, typeof(int)) : right;
                    var integerPower = Marker(nameof(ExpressoDbFunctions.SqlPowerInt), left, integerExponent);
                    if (integerPower is not null)
                    {
                        return integerPower;
                    }
                }

                var exponent = LinqEx.ConvertTo(right, typeof(double));
                var integerBase = Marker(nameof(ExpressoDbFunctions.SqlPower), left, exponent);
                if (integerBase is not null)
                {
                    return integerBase;
                }
            }

            var floatingExponent = LinqEx.ConvertTo(right, typeof(double));
            var baseValue = LinqEx.ConvertTo(left, typeof(double));
            baseValue = OracleNumericOperand(baseValue);
            floatingExponent = OracleNumericOperand(floatingExponent);

            return Marker(nameof(ExpressoDbFunctions.SqlPower), baseValue, floatingExponent) ?? base.Power(baseValue, floatingExponent);
        }

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

        private Expression OracleNumericOperand(Expression value) =>
            Provider == EfCoreProvider.Oracle ? new OracleNumericPromotion(this).Visit(value)! : value;

        private sealed class ParameterProbe : ExpressionVisitor
        {
            public bool Found { get; private set; }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                Found = true;
                return node;
            }
        }

        // CLR promotion is necessary for the expression types, but Oracle must keep NUMBER integer operands
        // inside computed POWER arguments. Actual double columns have no integer conversion and are untouched.
        private sealed class OracleNumericPromotion : ExpressionVisitor
        {
            private readonly EfCoreExpressionToLinqTransformer _transformer;

            public OracleNumericPromotion(EfCoreExpressionToLinqTransformer transformer) => _transformer = transformer;

            protected override Expression VisitUnary(UnaryExpression node)
            {
                var operand = Visit(node.Operand);
                return node.NodeType == ExpressionType.Convert && node.Type == typeof(double)
                    && (operand.Type == typeof(int) || operand.Type == typeof(byte))
                    ? _transformer.Marker(nameof(ExpressoDbFunctions.OracleNumber), operand)!
                    : node.Update(operand);
            }
        }
    }
}
