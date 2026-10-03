using System.Linq.Expressions;

namespace Expresso.Rendering.Linq
{
    /// <summary>
    /// In-memory profile (LINQ-to-objects): PostgreSQL reference semantics through <see cref="ExpressoFunctions"/>
    /// and ordinal string operations. Use with <see cref="IEnumerable{T}"/>.
    /// </summary>
    public class InMemoryExpressionToLinqTransformer : ExpressionToLinqTransformerBase
    {
        private static readonly Type S = typeof(string);
        private static readonly Expression Ordinal = Expression.Constant(StringComparison.Ordinal);

        /// <inheritdoc />
        protected override Expression Round(Expression value, Expression? digits) =>
            Call(nameof(ExpressoFunctions.Round), value, digits ?? Expression.Constant(0));

        /// <inheritdoc />
        protected override Expression StartsWith(Expression source, Expression pattern) =>
            Expression.Call(source, LinqEx.Method(S, nameof(string.StartsWith), S, typeof(StringComparison)), pattern, Ordinal);

        /// <inheritdoc />
        protected override Expression EndsWith(Expression source, Expression pattern) =>
            Expression.Call(source, LinqEx.Method(S, nameof(string.EndsWith), S, typeof(StringComparison)), pattern, Ordinal);

        /// <inheritdoc />
        protected override Expression Contains(Expression source, Expression pattern) =>
            Expression.GreaterThanOrEqual(IndexOf(source, pattern), Expression.Constant(0));

        /// <inheritdoc />
        protected override Expression Substring(Expression source, Expression start, Expression length) =>
            Call(nameof(ExpressoFunctions.Substring), source, start, length);

        /// <inheritdoc />
        protected override Expression Left(Expression source, Expression length) =>
            Call(nameof(ExpressoFunctions.Left), source, length);

        /// <inheritdoc />
        protected override Expression Right(Expression source, Expression length) =>
            Call(nameof(ExpressoFunctions.Right), source, length);

        /// <inheritdoc />
        protected override LinqNode Concat(IReadOnlyList<LinqNode> arguments) => ConcatNullAsEmpty(arguments);

        /// <inheritdoc />
        protected override Expression Lower(Expression source) =>
            Expression.Call(source, LinqEx.Method(S, nameof(string.ToLowerInvariant)));

        /// <inheritdoc />
        protected override Expression Upper(Expression source) =>
            Expression.Call(source, LinqEx.Method(S, nameof(string.ToUpperInvariant)));

        /// <inheritdoc />
        protected override Expression Trim(Expression source) => Call(nameof(ExpressoFunctions.Trim), source);

        /// <inheritdoc />
        protected override Expression LTrim(Expression source) => Call(nameof(ExpressoFunctions.LTrim), source);

        /// <inheritdoc />
        protected override Expression RTrim(Expression source) => Call(nameof(ExpressoFunctions.RTrim), source);

        /// <inheritdoc />
        protected override Expression Replace(Expression source, Expression oldValue, Expression newValue) =>
            Call(nameof(ExpressoFunctions.Replace), source, oldValue, newValue);

        /// <inheritdoc />
        protected override Expression IndexOf(Expression source, Expression find) =>
            Expression.Call(source, LinqEx.Method(S, nameof(string.IndexOf), S, typeof(StringComparison)), find, Ordinal);

        /// <inheritdoc />
        protected override Expression DateAdd(LinqDatePart part, Expression value, Expression amount) =>
            value.Type == typeof(TimeSpan)
                ? Call(nameof(ExpressoFunctions.AddTimeOfDay), value, TimeSpanOf(part, amount))
                : base.DateAdd(part, value, amount);

        /// <inheritdoc />
        protected override Expression Min(Expression items, LambdaExpression selector) =>
            GenericCall(nameof(ExpressoFunctions.Min), items, selector);

        /// <inheritdoc />
        protected override Expression Max(Expression items, LambdaExpression selector) =>
            GenericCall(nameof(ExpressoFunctions.Max), items, selector);

        private static Expression Call(string name, params Expression[] arguments) =>
            Expression.Call(LinqEx.Method(typeof(ExpressoFunctions), name, arguments.Select(a => a.Type).ToArray()), arguments);

        private static Expression GenericCall(string name, Expression items, LambdaExpression selector) =>
            Expression.Call(typeof(ExpressoFunctions), name, new[] { selector.Parameters[0].Type, selector.ReturnType }, items, selector);
    }
}
