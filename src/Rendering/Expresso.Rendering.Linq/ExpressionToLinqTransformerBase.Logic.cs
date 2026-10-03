using System.Linq.Expressions;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using V = Expresso.Core.CriteriaExpressions.IExpressoVisitor<Expresso.Rendering.Linq.LinqScope, Expresso.Rendering.Linq.LinqNode>;

namespace Expresso.Rendering.Linq
{
    public abstract partial class ExpressionToLinqTransformerBase
    {
        /// <summary>
        /// SQL comparison: TRUE/FALSE only when both operands are not NULL, otherwise unknown.
        /// Numeric operands are promoted first.
        /// </summary>
        protected static LinqNode Compare(ExpressionType op, LinqNode left, LinqNode right)
        {
            var (l, r) = Promote(left, right);
            var guard = LinqEx.AndAlso(l.NotNull, r.NotNull);
            var whenTrue = Expression.MakeBinary(op, l.Value, r.Value);
            var whenFalse = Expression.MakeBinary(Complement(op), l.Value, r.Value);
            return LinqNode.Boolean(LinqEx.AndAlso(guard, whenTrue), LinqEx.AndAlso(guard, whenFalse), l.IsNull is null && r.IsNull is null);
        }

        /// <summary>Boolean test over non-null operands; unknown when any operand is NULL.</summary>
        protected static LinqNode Test(Expression test, params LinqNode[] operands)
        {
            var guard = LinqEx.AndAlso(operands.Select(o => o.NotNull).ToArray());
            var anyNull = LinqEx.AnyNull(operands);
            return LinqNode.Boolean(LinqEx.AndAlso(guard, test), LinqEx.AndAlso(guard, LinqEx.Not(test)), anyNull is null);
        }

        private static ExpressionType Complement(ExpressionType op) => op switch
        {
            ExpressionType.Equal => ExpressionType.NotEqual,
            ExpressionType.NotEqual => ExpressionType.Equal,
            ExpressionType.GreaterThan => ExpressionType.LessThanOrEqual,
            ExpressionType.GreaterThanOrEqual => ExpressionType.LessThan,
            ExpressionType.LessThan => ExpressionType.GreaterThanOrEqual,
            ExpressionType.LessThanOrEqual => ExpressionType.GreaterThan,
            _ => throw new ArgumentOutOfRangeException(nameof(op)),
        };

        private LinqNode Comparison(ComparisonFunction node, ExpressionType op, LinqScope s) =>
            Compare(op, Visit(node.Arguments[0], s), Visit(node.Arguments[1], s));

        private List<LinqNode> VisitAll(IEnumerable<AbstractExpression> arguments, LinqScope s) =>
            arguments.Select(a => Visit(a, s)).ToList();

        LinqNode V.VisitAnd(AndFunc node, LinqScope s)
        {
            var args = VisitAll(node.Arguments, s);
            return LinqNode.Boolean(
                LinqEx.AndAlso(args.Select(a => a.WhenTrue).ToArray()),
                LinqEx.OrElse(args.Select(a => a.WhenFalse).ToArray()),
                args.All(a => a.IsNull is null));
        }

        LinqNode V.VisitOr(OrFunc node, LinqScope s)
        {
            var args = VisitAll(node.Arguments, s);
            return LinqNode.Boolean(
                LinqEx.OrElse(args.Select(a => a.WhenTrue).ToArray()),
                LinqEx.AndAlso(args.Select(a => a.WhenFalse).ToArray()),
                args.All(a => a.IsNull is null));
        }

        LinqNode V.VisitNot(NotFunc node, LinqScope s)
        {
            var arg = Visit(node.Arguments[0], s);
            return LinqNode.Boolean(arg.WhenFalse, arg.WhenTrue, arg.IsNull is null);
        }

        LinqNode V.VisitEq(EqFunc node, LinqScope s) => Comparison(node, ExpressionType.Equal, s);
        LinqNode V.VisitNeq(NeqFunc node, LinqScope s) => Comparison(node, ExpressionType.NotEqual, s);
        LinqNode V.VisitGt(GtFunc node, LinqScope s) => Comparison(node, ExpressionType.GreaterThan, s);
        LinqNode V.VisitGte(GteFunc node, LinqScope s) => Comparison(node, ExpressionType.GreaterThanOrEqual, s);
        LinqNode V.VisitLt(LtFunc node, LinqScope s) => Comparison(node, ExpressionType.LessThan, s);
        LinqNode V.VisitLte(LteFunc node, LinqScope s) => Comparison(node, ExpressionType.LessThanOrEqual, s);

        LinqNode V.VisitIn(InFunc node, LinqScope s)
        {
            var args = VisitAll(node.Arguments, s);
            var eqs = args.Skip(1).Select(a => Compare(ExpressionType.Equal, args[0], a)).ToList();
            return LinqNode.Boolean(
                LinqEx.OrElse(eqs.Select(e => e.WhenTrue).ToArray()),
                LinqEx.AndAlso(eqs.Select(e => e.WhenFalse).ToArray()),
                eqs.All(e => e.IsNull is null));
        }

        LinqNode V.VisitIsNull(IsNullFunc node, LinqScope s)
        {
            var arg = Visit(node.Arguments[0], s);
            return LinqNode.Boolean(arg.IsNull ?? LinqEx.False, arg.NotNull ?? LinqEx.True, neverUnknown: true);
        }
    }
}
