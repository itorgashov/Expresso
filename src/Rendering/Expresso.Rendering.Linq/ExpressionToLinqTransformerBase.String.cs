using System.Linq.Expressions;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using V = Expresso.Core.CriteriaExpressions.IExpressoVisitor<Expresso.Rendering.Linq.LinqScope, Expresso.Rendering.Linq.LinqNode>;

namespace Expresso.Rendering.Linq
{
    public abstract partial class ExpressionToLinqTransformerBase
    {
        private static readonly Type S = typeof(string);

        /// <summary><c>startswith</c> (SQL <c>LIKE 'p%'</c>). Default <c>string.StartsWith(string)</c>.</summary>
        protected virtual Expression StartsWith(Expression source, Expression pattern) =>
            Expression.Call(source, LinqEx.Method(S, nameof(string.StartsWith), S), pattern);

        /// <summary><c>endswith</c> (SQL <c>LIKE '%p'</c>). Default <c>string.EndsWith(string)</c>.</summary>
        protected virtual Expression EndsWith(Expression source, Expression pattern) =>
            Expression.Call(source, LinqEx.Method(S, nameof(string.EndsWith), S), pattern);

        /// <summary><c>contains</c> (SQL <c>LIKE '%p%'</c>). Default <c>string.Contains(string)</c>.</summary>
        protected virtual Expression Contains(Expression source, Expression pattern) =>
            Expression.Call(source, LinqEx.Method(S, nameof(string.Contains), S), pattern);

        /// <summary><c>substring</c> with a 1-based <paramref name="start"/>. Default <c>Substring(start - 1, length)</c>.</summary>
        protected virtual Expression Substring(Expression source, Expression start, Expression length) =>
            Expression.Call(source, LinqEx.Method(S, nameof(string.Substring), typeof(int), typeof(int)),
                Expression.Subtract(start, Expression.Constant(1)), length);

        /// <summary><c>left</c>. Default <c>Substring(0, length)</c>.</summary>
        protected virtual Expression Left(Expression source, Expression length) =>
            Expression.Call(source, LinqEx.Method(S, nameof(string.Substring), typeof(int), typeof(int)), Expression.Constant(0), length);

        /// <summary><c>right</c>. Default <c>s.Length &lt;= n ? s : s.Substring(s.Length - n, n)</c>.</summary>
        protected virtual Expression Right(Expression source, Expression length)
        {
            var sourceLength = Length(source);
            return Expression.Condition(
                Expression.LessThanOrEqual(sourceLength, length),
                source,
                Expression.Call(source, LinqEx.Method(S, nameof(string.Substring), typeof(int), typeof(int)),
                    Expression.Subtract(sourceLength, length), length));
        }

        /// <summary><c>concat</c>. Default: NULL when any argument is NULL, otherwise <c>string.Concat</c>.</summary>
        protected virtual LinqNode Concat(IReadOnlyList<LinqNode> arguments) =>
            Propagate(a => a.Aggregate(ConcatPair), arguments.ToArray());

        /// <summary>
        /// <c>concat</c> with NULL arguments as empty strings. The result is never NULL (SQL Server / PostgreSQL <c>CONCAT</c>),
        /// or with <paramref name="nullWhenAllNull"/> NULL exactly when every argument is NULL (Oracle <c>||</c>).
        /// </summary>
        protected static LinqNode ConcatNullAsEmpty(IReadOnlyList<LinqNode> arguments, bool nullWhenAllNull = false) =>
            LinqNode.Scalar(
                arguments.Select(a => (Expression)Expression.Coalesce(a.ToNullable(), Expression.Constant(string.Empty))).Aggregate(ConcatPair),
                nullWhenAllNull && arguments.All(a => a.IsNull is not null)
                    ? arguments.Select(a => a.IsNull!).Aggregate(Expression.AndAlso)
                    : null);

        private static Expression ConcatPair(Expression left, Expression right) =>
            Expression.Call(LinqEx.Method(S, nameof(string.Concat), S, S), left, right);

        /// <summary><c>lower</c>. Default <c>ToLower()</c>.</summary>
        protected virtual Expression Lower(Expression source) => Expression.Call(source, LinqEx.Method(S, nameof(string.ToLower)));

        /// <summary><c>upper</c>. Default <c>ToUpper()</c>.</summary>
        protected virtual Expression Upper(Expression source) => Expression.Call(source, LinqEx.Method(S, nameof(string.ToUpper)));

        /// <summary><c>trim</c> (spaces). Default <c>Trim()</c>.</summary>
        protected virtual Expression Trim(Expression source) => Expression.Call(source, LinqEx.Method(S, nameof(string.Trim)));

        /// <summary><c>ltrim</c> (spaces). Default <c>TrimStart()</c>.</summary>
        protected virtual Expression LTrim(Expression source) => TrimSide(source, nameof(string.TrimStart));

        /// <summary><c>rtrim</c> (spaces). Default <c>TrimEnd()</c>.</summary>
        protected virtual Expression RTrim(Expression source) => TrimSide(source, nameof(string.TrimEnd));

        /// <summary><c>replace</c>. Default <c>Replace(string, string)</c>.</summary>
        protected virtual Expression Replace(Expression source, Expression oldValue, Expression newValue) =>
            Expression.Call(source, LinqEx.Method(S, nameof(string.Replace), S, S), oldValue, newValue);

        /// <summary><c>len</c>. Default <c>Length</c>.</summary>
        protected virtual Expression Length(Expression source) => Expression.Property(source, nameof(string.Length));

        /// <summary>Zero-based <c>indexof</c>, <c>-1</c> when missing. Default <c>IndexOf(string)</c>.</summary>
        protected virtual Expression IndexOf(Expression source, Expression find) =>
            Expression.Call(source, LinqEx.Method(S, nameof(string.IndexOf), S), find);

        private static Expression TrimSide(Expression source, string methodName)
        {
#if NET6_0_OR_GREATER
            return Expression.Call(source, LinqEx.Method(S, methodName));
#else
            return Expression.Call(source, LinqEx.Method(S, methodName, typeof(char[])), Expression.NewArrayInit(typeof(char)));
#endif
        }

        private LinqNode LikeTest(AbstractFunction node, LinqScope s, Func<Expression, Expression, Expression> test)
        {
            var source = Visit(node.Arguments[0], s);
            var pattern = Visit(node.Arguments[1], s);
            return Test(test(source.Value, pattern.Value), source, pattern);
        }

        private LinqNode StringCall(AbstractFunction node, LinqScope s, Func<Expression[], Expression> compute) =>
            Propagate(compute, VisitAll(node.Arguments, s).ToArray());

        LinqNode V.VisitStrStartswith(StrStartswithFunc node, LinqScope s) => LikeTest(node, s, StartsWith);
        LinqNode V.VisitStrEndswith(StrEndswithFunc node, LinqScope s) => LikeTest(node, s, EndsWith);
        LinqNode V.VisitStrContains(StrContainsFunc node, LinqScope s) => LikeTest(node, s, Contains);
        LinqNode V.VisitSubString(SubStringFunc node, LinqScope s) => StringCall(node, s, a => Substring(a[0], a[1], a[2]));
        LinqNode V.VisitLeft(LeftFunc node, LinqScope s) => StringCall(node, s, a => Left(a[0], a[1]));
        LinqNode V.VisitRight(RightFunc node, LinqScope s) => StringCall(node, s, a => Right(a[0], a[1]));
        LinqNode V.VisitConcat(ConcatFunc node, LinqScope s) => Concat(VisitAll(node.Arguments, s));
        LinqNode V.VisitLower(LowerFunc node, LinqScope s) => StringCall(node, s, a => Lower(a[0]));
        LinqNode V.VisitUpper(UpperFunc node, LinqScope s) => StringCall(node, s, a => Upper(a[0]));
        LinqNode V.VisitTrim(TrimFunc node, LinqScope s) => StringCall(node, s, a => Trim(a[0]));
        LinqNode V.VisitLTrim(LTrimFunc node, LinqScope s) => StringCall(node, s, a => LTrim(a[0]));
        LinqNode V.VisitRTrim(RTrimFunc node, LinqScope s) => StringCall(node, s, a => RTrim(a[0]));
        LinqNode V.VisitReplace(ReplaceFunc node, LinqScope s) => StringCall(node, s, a => Replace(a[0], a[1], a[2]));
        LinqNode V.VisitLen(LenFunc node, LinqScope s) => StringCall(node, s, a => Length(a[0]));
        LinqNode V.VisitIndexOf(IndexOfFunc node, LinqScope s) => StringCall(node, s, a => IndexOf(a[0], a[1]));
    }
}
