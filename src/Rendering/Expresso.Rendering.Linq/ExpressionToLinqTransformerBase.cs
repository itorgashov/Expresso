using System.Linq.Expressions;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using V = Expresso.Core.CriteriaExpressions.IExpressoVisitor<Expresso.Rendering.Linq.LinqScope, Expresso.Rendering.Linq.LinqNode>;

namespace Expresso.Rendering.Linq
{
    /// <summary>
    /// IR walker that renders LINQ lambdas with the same null logic, type rules and parameterization as the SQL renderers.
    /// Function hooks are <c>protected virtual</c>; the defaults are the Queryable profile (BCL members that LINQ providers translate).
    /// </summary>
    public abstract partial class ExpressionToLinqTransformerBase : IExpressionToLinqTransformer, V
    {
        /// <inheritdoc />
        public Expression<Func<T, bool>> BuildPredicate<T>(FilterCriteria filter, LinqQueryMapping<T> mapping)
        {
            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            if (filter.Expression is null)
            {
                throw new ArgumentException("The expression of the filter criteria is null.", nameof(filter));
            }

            if (mapping is null)
            {
                throw new ArgumentNullException(nameof(mapping));
            }

            var item = Expression.Parameter(typeof(T), "e");
            var node = Visit(filter.Expression, new LinqScope(item, mapping));
            return Expression.Lambda<Func<T, bool>>(node.WhenTrue, item);
        }

        /// <inheritdoc />
        public IReadOnlyList<LinqSortKey> BuildSortKeys<T>(SortDirective sort, LinqQueryMapping<T> mapping)
        {
            if (sort is null)
            {
                throw new ArgumentNullException(nameof(sort));
            }

            if (sort.Items is null || sort.Items.Count == 0)
            {
                throw new ArgumentException("Sort directive must contain at least one item", nameof(sort));
            }

            if (mapping is null)
            {
                throw new ArgumentNullException(nameof(mapping));
            }

            var item = Expression.Parameter(typeof(T), "e");
            var scope = new LinqScope(item, mapping);
            var keys = new List<LinqSortKey>(sort.Items.Count);
            foreach (var sortItem in sort.Items)
            {
                if (sortItem.Expression is CollectionQuantifierFunction)
                {
                    throw new ArgumentException("Collections and collection quantifiers cannot be used as sort keys.");
                }

                var node = Visit(sortItem.Expression, scope);
                var key = sortItem.Expression is BooleanFunction
                    ? Expression.Condition(node.WhenTrue, Expression.Constant(1), Expression.Constant(0))
                    : node.ToNullable();
                keys.Add(new LinqSortKey(Expression.Lambda(key, item), sortItem.Direction));
            }

            return keys;
        }

        /// <summary>Renders <paramref name="expression"/> in <paramref name="scope"/>.</summary>
        protected LinqNode Visit(AbstractExpression expression, LinqScope scope) => expression.Accept(this, scope);

        /// <summary>A literal read from a captured <see cref="ParameterBox{T}"/>, so providers bind it as a parameter.</summary>
        protected static LinqNode Parameter(object value)
        {
            var box = Activator.CreateInstance(typeof(ParameterBox<>).MakeGenericType(value.GetType()), value);
            return LinqNode.Scalar(Expression.Field(Expression.Constant(box), nameof(ParameterBox<int>.Value)), null);
        }

        /// <summary>Converts a numeric node to <paramref name="type"/>, keeping its NULL condition.</summary>
        protected static LinqNode ConvertNode(LinqNode node, Type type) =>
            node.Type == type ? node : LinqNode.Scalar(Expression.Convert(node.Value, type), node.IsNull);

        /// <summary>SQL numeric promotion: <c>byte</c> becomes <c>int</c>; either side <c>double</c> makes both <c>double</c>.</summary>
        protected static (LinqNode Left, LinqNode Right) Promote(LinqNode left, LinqNode right)
        {
            if (!IsNumeric(left.Type) || !IsNumeric(right.Type))
            {
                return (left, right);
            }

            var target = left.Type == typeof(double) || right.Type == typeof(double) ? typeof(double) : typeof(int);
            return (ConvertNode(left, target), ConvertNode(right, target));
        }

        /// <summary>Unary promotion: <c>byte</c> becomes <c>int</c>.</summary>
        protected static LinqNode Promote(LinqNode node) =>
            node.Type == typeof(byte) ? ConvertNode(node, typeof(int)) : node;

        /// <summary>
        /// When a SQL function can return NULL even though its arguments are not NULL (for example <c>sqrt</c> of a negative
        /// value, or Oracle empty string), return a condition that is true when the computed <paramref name="value"/> is NULL.
        /// </summary>
        protected virtual Expression? ComputedNull(string function, IReadOnlyList<LinqNode> args, Expression value) => null;

        /// <summary>Builds a literal node. An empty Oracle string is NULL only inside scalar functions, not LIKE patterns.</summary>
        protected virtual LinqNode Literal(Literal node) => Parameter(node.Value);

        /// <summary>True when <paramref name="expression"/> is a captured <c>""</c> literal.</summary>
        protected static bool IsEmptyStringLiteral(Expression expression)
        {
            if (expression is not MemberExpression { Expression: ConstantExpression { Value: { } box } } member)
            {
                return false;
            }

            var field = box.GetType().GetField(member.Member.Name);
            return field?.GetValue(box) is string text && text.Length == 0;
        }

        /// <summary>
        /// <c>value IS NULL</c> so a provider that raises on the value still evaluates it inside <c>isnull</c>.
        /// </summary>
        protected static Expression ValueIsNull(Expression value)
        {
            var nullable = LinqEx.NullableType(value.Type);
            var lifted = value.Type == nullable ? value : Expression.Convert(value, nullable);
            return Expression.Equal(lifted, Expression.Constant(null, nullable));
        }

        /// <summary>
        /// NULL state inherited from arguments. Oracle <c>replace</c> inherits only the source: a NULL search is a no-op
        /// and a NULL replacement deletes matches.
        /// </summary>
        protected virtual Expression? NullFromArguments(string? function, IReadOnlyList<LinqNode> arguments) =>
            LinqEx.AnyNull(arguments);

        /// <summary>Combines argument NULL propagation with <see cref="ComputedNull"/>.</summary>
        protected Expression? CombineIsNull(string? function, IReadOnlyList<LinqNode> args, Expression value, Expression? fromArgs)
        {
            var computed = function is null ? null : ComputedNull(function, args, value);
            if (computed is ConstantExpression { Value: true })
            {
                return computed;
            }

            if (fromArgs is null && computed is null)
            {
                return null;
            }

            if (fromArgs is null)
            {
                return computed;
            }

            return computed is null ? fromArgs : Expression.OrElse(fromArgs, computed);
        }

        /// <summary>Applies <paramref name="compute"/>; NULL when any argument is NULL or <see cref="ComputedNull"/> holds.</summary>
        protected LinqNode Propagate(Func<Expression[], Expression> compute, params LinqNode[] arguments) =>
            Propagate(null, compute, arguments);

        /// <summary>Applies <paramref name="compute"/> with optional <paramref name="function"/> for <see cref="ComputedNull"/>.</summary>
        protected LinqNode Propagate(string? function, Func<Expression[], Expression> compute, params LinqNode[] arguments)
        {
            var values = arguments.Select(a => a.Value).ToArray();
            var value = compute(values);
            return LinqNode.Scalar(value, CombineIsNull(function, arguments, value, NullFromArguments(function, arguments)));
        }

        /// <summary>Called for each mapped field before it is used. The default accepts every field.</summary>
        protected virtual void ValidateField(Expression body)
        {
        }

        private static bool IsNumeric(Type type) =>
            type == typeof(byte) || type == typeof(int) || type == typeof(double);

        LinqNode V.VisitField(Field node, LinqScope s)
        {
            if (!s.Mapping.Fields.TryGetValue(node.Name, out var selector))
            {
                throw new ArgumentException($"No mapping for the {node.Name} field");
            }

            var body = LinqEx.Inline(selector, s.Item);
            ValidateField(body);
            var underlying = Nullable.GetUnderlyingType(body.Type);
            if ((underlying ?? body.Type) != node.ReturnType)
            {
                throw new ArgumentException(
                    $"Field '{node.Name}' is mapped to {body.Type.Name} but its catalog type is {node.ReturnType.Name}.");
            }

            if (underlying is not null)
            {
                return LinqNode.Scalar(Expression.Property(body, "Value"), Expression.Equal(body, Expression.Constant(null, body.Type)), body);
            }

            return body.Type.IsValueType
                ? LinqNode.Scalar(body, null)
                : LinqNode.Scalar(body, Expression.Equal(body, Expression.Constant(null, body.Type)), body);
        }

        LinqNode V.VisitLiteral(Literal node, LinqScope s) => Literal(node);

        LinqNode V.VisitCollectionRef(CollectionRef node, LinqScope s) =>
            throw new NotSupportedException($"Expression type '{nameof(CollectionRef)}' is not supported.");
    }
}
