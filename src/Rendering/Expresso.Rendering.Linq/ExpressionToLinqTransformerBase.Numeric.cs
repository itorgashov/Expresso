using System.Linq.Expressions;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using V = Expresso.Core.CriteriaExpressions.IExpressoVisitor<Expresso.Rendering.Linq.LinqScope, Expresso.Rendering.Linq.LinqNode>;

namespace Expresso.Rendering.Linq
{
    public abstract partial class ExpressionToLinqTransformerBase
    {
        /// <summary><c>abs</c> over an <c>int</c> or <c>double</c> value. Default <c>Math.Abs</c>.</summary>
        protected virtual Expression Abs(Expression value) =>
            Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Abs), value.Type), value);

        /// <summary><c>add</c> over promoted operands.</summary>
        protected virtual Expression Add(Expression left, Expression right) => Expression.Add(left, right);

        /// <summary><c>sub</c> over promoted operands.</summary>
        protected virtual Expression Subtract(Expression left, Expression right) => Expression.Subtract(left, right);

        /// <summary><c>mult</c> over promoted operands.</summary>
        protected virtual Expression Multiply(Expression left, Expression right) => Expression.Multiply(left, right);

        /// <summary><c>div</c> over promoted operands; <c>int / int</c> truncates.</summary>
        protected virtual Expression Divide(Expression left, Expression right) => Expression.Divide(left, right);

        /// <summary><c>mod</c> over promoted operands.</summary>
        protected virtual Expression Modulo(Expression left, Expression right) => Expression.Modulo(left, right);

        /// <summary>
        /// <c>floor</c>. The argument keeps its numeric type so SQL Server can return <c>int</c>.
        /// Default converts to <c>double</c> and calls <c>Math.Floor</c>.
        /// </summary>
        protected virtual Expression Floor(Expression value) =>
            Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Floor), typeof(double)), LinqEx.ConvertTo(value, typeof(double)));

        /// <summary>
        /// <c>ceiling</c>. The argument keeps its numeric type so SQL Server can return <c>int</c>.
        /// Default converts to <c>double</c> and calls <c>Math.Ceiling</c>.
        /// </summary>
        protected virtual Expression Ceiling(Expression value) =>
            Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Ceiling), typeof(double)), LinqEx.ConvertTo(value, typeof(double)));

        /// <summary>
        /// <c>round</c>. The value keeps its numeric type so SQL Server can return <c>int</c>.
        /// Default converts to <c>double</c> and calls <c>Math.Round</c>.
        /// </summary>
        protected virtual Expression Round(Expression value, Expression? digits)
        {
            var number = LinqEx.ConvertTo(value, typeof(double));
            return digits is null
                ? Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Round), typeof(double)), number)
                : Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Round), typeof(double), typeof(int)), number, digits);
        }

        /// <summary><c>sign</c> over an <c>int</c> or <c>double</c> value. Default <c>Math.Sign</c>.</summary>
        protected virtual Expression Sign(Expression value) =>
            Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Sign), value.Type), value);

        /// <summary>
        /// <c>power</c>. Arguments keep their original numeric types so a provider can preserve an integer base.
        /// Default converts both to <c>double</c> and calls <c>Math.Pow</c>.
        /// </summary>
        protected virtual Expression Power(Expression left, Expression right) =>
            Expression.Call(
                LinqEx.Method(typeof(Math), nameof(Math.Pow), typeof(double), typeof(double)),
                LinqEx.ConvertTo(left, typeof(double)),
                LinqEx.ConvertTo(right, typeof(double)));

        /// <summary><c>sqrt</c> over a <c>double</c> value. Default <c>Math.Sqrt</c>.</summary>
        protected virtual Expression Sqrt(Expression value) =>
            Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Sqrt), typeof(double)), value);

        private LinqNode Unary(AbstractFunction node, LinqScope s, Func<Expression, Expression> compute, Type? argumentType = null) =>
            Unary(node, s, null!, compute, argumentType);

        private LinqNode Unary(AbstractFunction node, LinqScope s, string? function, Func<Expression, Expression> compute, Type? argumentType = null)
        {
            var arg = Visit(node.Arguments[0], s);
            arg = argumentType is null ? Promote(arg) : ConvertNode(arg, argumentType);
            return Propagate(function, a => compute(a[0]), arg);
        }

        private LinqNode Binary(NumericArithFunction node, LinqScope s, Func<Expression, Expression, Expression> compute, Type? argumentType = null, string? function = null)
        {
            var left = Visit(node.Arguments[0], s);
            var right = Visit(node.Arguments[1], s);
            (left, right) = argumentType is null
                ? Promote(left, right)
                : (ConvertNode(left, argumentType), ConvertNode(right, argumentType));
            return Propagate(function, a => compute(a[0], a[1]), left, right);
        }

        /// <summary>SQL <c>CASE WHEN a op b THEN a ELSE b END</c>: when the comparison is not TRUE the result is <c>b</c>.</summary>
        private LinqNode Pick(NumericArithFunction node, ExpressionType op, LinqScope s)
        {
            var (left, right) = Promote(Visit(node.Arguments[0], s), Visit(node.Arguments[1], s));
            var chooseLeft = Compare(op, left, right).WhenTrue;
            var isNull = right.IsNull is null ? null : LinqEx.AndAlso(LinqEx.Not(chooseLeft), right.IsNull);
            return LinqNode.Scalar(Expression.Condition(chooseLeft, left.Value, right.Value), isNull);
        }

        LinqNode V.VisitAbs(AbsFunc node, LinqScope s) => Unary(node, s, "abs", Abs);
        LinqNode V.VisitAdd(AddFunc node, LinqScope s) => Binary(node, s, Add, function: "add");
        LinqNode V.VisitSub(SubFunc node, LinqScope s) => Binary(node, s, Subtract, function: "sub");
        LinqNode V.VisitMult(MultFunc node, LinqScope s) => Binary(node, s, Multiply, function: "mult");
        LinqNode V.VisitDiv(DivFunc node, LinqScope s) => Binary(node, s, Divide, function: "div");
        LinqNode V.VisitMod(ModFunc node, LinqScope s) => Binary(node, s, Modulo, function: "mod");
        LinqNode V.VisitFloor(FloorFunc node, LinqScope s) => Unary(node, s, Floor);
        LinqNode V.VisitCeiling(CeilingFunc node, LinqScope s) => Unary(node, s, Ceiling);
        LinqNode V.VisitSign(SignFunc node, LinqScope s) => Unary(node, s, Sign);
        LinqNode V.VisitPower(PowerFunc node, LinqScope s)
        {
            var left = Visit(node.Arguments[0], s);
            var right = Visit(node.Arguments[1], s);
            return Propagate("power", a => Power(a[0], a[1]), left, right);
        }
        LinqNode V.VisitSqrt(SqrtFunc node, LinqScope s) => Unary(node, s, "sqrt", Sqrt, typeof(double));
        LinqNode V.VisitMin(MinFunc node, LinqScope s) => Pick(node, ExpressionType.LessThan, s);
        LinqNode V.VisitMax(MaxFunc node, LinqScope s) => Pick(node, ExpressionType.GreaterThan, s);

        LinqNode V.VisitRound(RoundFunc node, LinqScope s)
        {
            var value = Promote(Visit(node.Arguments[0], s));
            if (node.Arguments.Count == 1)
            {
                return Propagate("round", a => Round(a[0], null), value);
            }

            var digits = Visit(node.Arguments[1], s);
            return Propagate("round", a => Round(a[0], a[1]), value, digits);
        }
    }
}
