using System.Linq.Expressions;
using System.Reflection;

namespace Expresso.Rendering.Linq
{
    /// <summary>Expression-building helpers shared by the transformers.</summary>
    public static class LinqEx
    {
        /// <summary>Constant <see langword="true"/>.</summary>
        public static readonly Expression True = Expression.Constant(true);

        /// <summary>Constant <see langword="false"/>.</summary>
        public static readonly Expression False = Expression.Constant(false);

        /// <summary><c>a &amp;&amp; b &amp;&amp; …</c>, skipping <see langword="null"/> and constant-true operands.</summary>
        public static Expression AndAlso(params Expression?[] operands)
        {
            Expression? result = null;
            foreach (var operand in operands)
            {
                if (operand is null || IsConstant(operand, true))
                {
                    continue;
                }

                if (IsConstant(operand, false))
                {
                    return False;
                }

                result = result is null ? operand : Expression.AndAlso(result, operand);
            }

            return result ?? True;
        }

        /// <summary><c>a || b || …</c>, skipping <see langword="null"/> and constant-false operands.</summary>
        public static Expression OrElse(params Expression?[] operands)
        {
            Expression? result = null;
            foreach (var operand in operands)
            {
                if (operand is null || IsConstant(operand, false))
                {
                    continue;
                }

                if (IsConstant(operand, true))
                {
                    return True;
                }

                result = result is null ? operand : Expression.OrElse(result, operand);
            }

            return result ?? False;
        }

        /// <summary><c>!e</c> with constant folding and double-negation removal.</summary>
        public static Expression Not(Expression operand)
        {
            if (IsConstant(operand, true))
            {
                return False;
            }

            if (IsConstant(operand, false))
            {
                return True;
            }

            if (operand is BinaryExpression { NodeType: ExpressionType.Equal, Right: ConstantExpression { Value: null } } isNull)
            {
                return Expression.NotEqual(isNull.Left, isNull.Right);
            }

            return operand.NodeType == ExpressionType.Not && operand.Type == typeof(bool)
                ? ((UnaryExpression)operand).Operand
                : Expression.Not(operand);
        }

        /// <summary>OR of the NULL conditions, or <see langword="null"/> when no operand can be null.</summary>
        public static Expression? AnyNull(IEnumerable<LinqNode> nodes)
        {
            var conditions = nodes.Select(n => n.IsNull).Where(c => c is not null).ToArray();
            return conditions.Length == 0 ? null : OrElse(conditions);
        }

        /// <summary><see cref="Nullable{T}"/> of a non-nullable value type; other types unchanged.</summary>
        public static Type NullableType(Type type) =>
            type.IsValueType && Nullable.GetUnderlyingType(type) is null
                ? typeof(Nullable<>).MakeGenericType(type)
                : type;

        /// <summary>Converts <paramref name="expression"/> to <paramref name="type"/> unless it already has it.</summary>
        public static Expression ConvertTo(Expression expression, Type type) =>
            expression.Type == type ? expression : Expression.Convert(expression, type);

        /// <summary>Public instance or static method with the exact parameter types.</summary>
        public static MethodInfo Method(Type type, string name, params Type[] parameterTypes)
        {
            var method = type.GetMethod(name, parameterTypes);
            return method is not null && method.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes)
                ? method
                : throw new MissingMethodException(type.FullName, name);
        }

        /// <summary>
        /// <c>Enumerable.name&lt;genericArgs&gt;(source)</c> or, with two parameters,
        /// <c>Enumerable.name&lt;genericArgs&gt;(source, Func&lt;…&gt;)</c>.
        /// </summary>
        public static MethodInfo EnumerableMethod(string name, int parameterCount, params Type[] genericArguments) =>
            typeof(Enumerable).GetMethods()
                .Single(m => m.Name == name
                    && m.IsGenericMethodDefinition
                    && m.GetGenericArguments().Length == genericArguments.Length
                    && m.GetParameters().Length == parameterCount
                    && (parameterCount == 1 || IsFunc(m.GetParameters()[1].ParameterType)))
                .MakeGenericMethod(genericArguments);

        /// <summary><c>Enumerable.name&lt;TSource&gt;(source, Func&lt;TSource, resultType&gt;)</c> (Sum, Average).</summary>
        public static MethodInfo EnumerableSelectorMethod(string name, Type sourceType, Type resultType) =>
            typeof(Enumerable).GetMethods()
                .Single(m => m.Name == name
                    && m.IsGenericMethodDefinition
                    && m.GetGenericArguments().Length == 1
                    && m.GetParameters().Length == 2
                    && IsFunc(m.GetParameters()[1].ParameterType)
                    && m.GetParameters()[1].ParameterType.GetGenericArguments()[1] == resultType)
                .MakeGenericMethod(sourceType);

        /// <summary>Body of <paramref name="lambda"/> with its single parameter replaced by <paramref name="replacement"/>.</summary>
        public static Expression Inline(LambdaExpression lambda, Expression replacement) =>
            new ParameterReplacer(lambda.Parameters[0], replacement).Visit(lambda.Body);

        private static bool IsFunc(Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Func<,>);

        private static bool IsConstant(Expression expression, bool value) =>
            expression is ConstantExpression { Value: bool b } && b == value;

        private sealed class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _parameter;
            private readonly Expression _replacement;

            public ParameterReplacer(ParameterExpression parameter, Expression replacement)
            {
                _parameter = parameter;
                _replacement = replacement;
            }

            protected override Expression VisitParameter(ParameterExpression node) =>
                node == _parameter ? _replacement : base.VisitParameter(node);
        }
    }
}
