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

        /// <summary><c>floor</c> over a <c>double</c> value. Default <c>Math.Floor</c>.</summary>
        protected virtual Expression Floor(Expression value) =>
            Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Floor), typeof(double)), value);

        /// <summary><c>ceiling</c> over a <c>double</c> value. Default <c>Math.Ceiling</c>.</summary>
        protected virtual Expression Ceiling(Expression value) =>
            Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Ceiling), typeof(double)), value);

        /// <summary><c>round</c> over a <c>double</c> value and optional <c>int</c> digits. Default <c>Math.Round</c> (the provider rounds like SQL <c>ROUND</c>).</summary>
        protected virtual Expression Round(Expression value, Expression? digits) =>
            digits is null
                ? Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Round), typeof(double)), value)
                : Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Round), typeof(double), typeof(int)), value, digits);

        /// <summary><c>sign</c> over an <c>int</c> or <c>double</c> value. Default <c>Math.Sign</c>.</summary>
        protected virtual Expression Sign(Expression value) =>
            Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Sign), value.Type), value);

        /// <summary><c>power</c> over <c>double</c> operands. Default <c>Math.Pow</c>.</summary>
        protected virtual Expression Power(Expression left, Expression right) =>
            Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Pow), typeof(double), typeof(double)), left, right);

        /// <summary><c>sqrt</c> over a <c>double</c> value. Default <c>Math.Sqrt</c>.</summary>
        protected virtual Expression Sqrt(Expression value) =>
            Expression.Call(LinqEx.Method(typeof(Math), nameof(Math.Sqrt), typeof(double)), value);

        /// <summary>Applies <paramref name="compute"/> to the non-null values; NULL when any argument is NULL.</summary>
        protected static LinqNode Propagate(Func<Expression[], Expression> compute, params LinqNode[] arguments) =>
            LinqNode.Scalar(compute(arguments.Select(a => a.Value).ToArray()), LinqEx.AnyNull(arguments));

        private LinqNode Unary(AbstractFunction node, LinqScope s, Func<Expression, Expression> compute, Type? argumentType = null)
        {
            var arg = Visit(node.Arguments[0], s);
            arg = argumentType is null ? Promote(arg) : ConvertNode(arg, argumentType);
            return Propagate(a => compute(a[0]), arg);
        }

        private LinqNode Binary(NumericArithFunction node, LinqScope s, Func<Expression, Expression, Expression> compute, Type? argumentType = null)
        {
            var left = Visit(node.Arguments[0], s);
            var right = Visit(node.Arguments[1], s);
            (left, right) = argumentType is null
                ? Promote(left, right)
                : (ConvertNode(left, argumentType), ConvertNode(right, argumentType));
            return Propagate(a => compute(a[0], a[1]), left, right);
        }

        /// <summary>SQL <c>CASE WHEN a op b THEN a ELSE b END</c>: when the comparison is not TRUE the result is <c>b</c>.</summary>
        private LinqNode Pick(NumericArithFunction node, ExpressionType op, LinqScope s)
        {
            var (left, right) = Promote(Visit(node.Arguments[0], s), Visit(node.Arguments[1], s));
            var chooseLeft = Compare(op, left, right).WhenTrue;
            var isNull = right.IsNull is null ? null : LinqEx.AndAlso(LinqEx.Not(chooseLeft), right.IsNull);
            return LinqNode.Scalar(Expression.Condition(chooseLeft, left.Value, right.Value), isNull);
        }

        LinqNode V.VisitAbs(AbsFunc node, LinqScope s) => Unary(node, s, Abs);
        LinqNode V.VisitAdd(AddFunc node, LinqScope s) => Binary(node, s, Add);
        LinqNode V.VisitSub(SubFunc node, LinqScope s) => Binary(node, s, Subtract);
        LinqNode V.VisitMult(MultFunc node, LinqScope s) => Binary(node, s, Multiply);
        LinqNode V.VisitDiv(DivFunc node, LinqScope s) => Binary(node, s, Divide);
        LinqNode V.VisitMod(ModFunc node, LinqScope s) => Binary(node, s, Modulo);
        LinqNode V.VisitFloor(FloorFunc node, LinqScope s) => Unary(node, s, Floor, typeof(double));
        LinqNode V.VisitCeiling(CeilingFunc node, LinqScope s) => Unary(node, s, Ceiling, typeof(double));
        LinqNode V.VisitSign(SignFunc node, LinqScope s) => Unary(node, s, Sign);
        LinqNode V.VisitPower(PowerFunc node, LinqScope s) => Binary(node, s, Power, typeof(double));
        LinqNode V.VisitSqrt(SqrtFunc node, LinqScope s) => Unary(node, s, Sqrt, typeof(double));
        LinqNode V.VisitMin(MinFunc node, LinqScope s) => Pick(node, ExpressionType.LessThan, s);
        LinqNode V.VisitMax(MaxFunc node, LinqScope s) => Pick(node, ExpressionType.GreaterThan, s);

        LinqNode V.VisitRound(RoundFunc node, LinqScope s)
        {
            var value = ConvertNode(Visit(node.Arguments[0], s), typeof(double));
            if (node.Arguments.Count == 1)
            {
                return Propagate(a => Round(a[0], null), value);
            }

            var digits = Visit(node.Arguments[1], s);
            return Propagate(a => Round(a[0], a[1]), value, digits);
        }
    }
}
