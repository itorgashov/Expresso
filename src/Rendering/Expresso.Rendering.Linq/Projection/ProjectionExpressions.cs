using System.Linq.Expressions;
using Expresso.Core.Projection;

namespace Expresso.Rendering.Linq;

internal static class ProjectionExpressions
{
    internal static Expression Replace(LambdaExpression lambda, Expression replacement) =>
        new ReplaceParameter(lambda.Parameters[0], replacement).Visit(lambda.Body)!;

    internal static MemberInitExpression Key(IEnumerable<LambdaExpression> selectors, Expression parameter)
    {
        var values = selectors.Select(s => Replace(s, parameter)).ToArray();
        if (values.Length == 0) throw new ArgumentException("Projection mappings require keys.");
        var type = typeof(ProjectionKeyLeaf<>).MakeGenericType(values[values.Length - 1].Type);
        MemberInitExpression result = Expression.MemberInit(Expression.New(type),
            Expression.Bind(type.GetProperty("Value")!, values[values.Length - 1]));
        for (var i = values.Length - 2; i >= 0; i--)
        {
            type = typeof(ProjectionKeyPair<,>).MakeGenericType(values[i].Type, result.Type);
            result = Expression.MemberInit(Expression.New(type),
                Expression.Bind(type.GetProperty("Value")!, values[i]), Expression.Bind(type.GetProperty("Rest")!, result));
        }
        return result;
    }

    internal static Expression<Func<T, bool>> MatchKeys<T>(
        IReadOnlyList<(string Name, LambdaExpression Source)> selectors, IReadOnlyList<CompositeProjectionKey> keys)
    {
        var parameter = Expression.Parameter(typeof(T), "parent");
        if (selectors.Count == 1 && selectors[0].Source.ReturnType != typeof(byte[]))
        {
            var member = Replace(selectors[0].Source, parameter);
            var values = Array.CreateInstance(member.Type, keys.Count);
            for (var i = 0; i < keys.Count; i++) values.SetValue(keys[i][0], i);
            var box = typeof(ParameterBox<>).MakeGenericType(values.GetType());
            var capture = Activator.CreateInstance(box, new object[] { values })!;
            var list = Expression.Field(Expression.Constant(capture), "Value");
            return Expression.Lambda<Func<T, bool>>(Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains),
                new[] { member.Type }, list, member), parameter);
        }
        var matches = new List<Expression>();
        foreach (var key in keys)
        {
            Expression? match = null;
            for (var i = 0; i < selectors.Count; i++)
            {
                var member = Replace(selectors[i].Source, parameter);
                var type = member.Type;
                var box = typeof(ParameterBox<>).MakeGenericType(type);
                var capture = Activator.CreateInstance(box, new[] { key[i] })!;
                var value = Expression.Field(Expression.Constant(capture), "Value");
                // Keep an Equal node for provider SQL translation, with structural execution for LINQ-to-objects.
                var equals = EqualKey(member, value);
                match = match is null ? equals : Expression.AndAlso(match, equals);
            }
            matches.Add(match!);
        }
        // Balanced OR avoids provider/visitor stack overflow for large batches.
        Expression Or(int start, int count) => count == 1 ? matches[start] :
            Expression.OrElse(Or(start, count / 2), Or(start + count / 2, count - count / 2));
        return Expression.Lambda<Func<T, bool>>(Or(0, matches.Count), parameter);
    }

    internal static BinaryExpression EqualKey(Expression left, Expression right) => left.Type == typeof(byte[])
        ? Expression.Equal(left, right, false,
            typeof(ProjectionExpressions).GetMethod(nameof(BinaryEquals), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
        : Expression.Equal(left, right);

    private static bool BinaryEquals(byte[]? left, byte[]? right) => left is not null && right is not null && left.SequenceEqual(right);

    private sealed class ReplaceParameter : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;
        private readonly Expression _replacement;
        public ReplaceParameter(ParameterExpression parameter, Expression replacement)
        {
            _parameter = parameter;
            _replacement = replacement;
        }
        protected override Expression VisitParameter(ParameterExpression node) => node == _parameter ? _replacement : node;
    }
}

internal interface IProjectionKeyValues { void Append(List<object?> values); }
internal sealed class ProjectionKeyLeaf<T> : IProjectionKeyValues
{
    public T Value { get; set; } = default!;
    public void Append(List<object?> values) => values.Add(Value);
}
internal sealed class ProjectionKeyPair<T, TRest> : IProjectionKeyValues where TRest : IProjectionKeyValues
{
    public T Value { get; set; } = default!;
    public TRest Rest { get; set; } = default!;
    public void Append(List<object?> values)
    {
        values.Add(Value);
        Rest.Append(values);
    }
}
internal interface IProjectedRow
{
    object ItemObject { get; }
    CompositeProjectionKey KeyValue { get; }
    CompositeProjectionKey? ParentValue { get; }
}
internal sealed class ProjectionRow<TResult, TKey, TParent> : IProjectedRow
    where TKey : IProjectionKeyValues where TParent : IProjectionKeyValues
{
    public TResult Item { get; set; } = default!;
    public TKey Key { get; set; } = default!;
    public TParent Parent { get; set; } = default!;
    public object ItemObject => Item!;
    public CompositeProjectionKey KeyValue => Convert(Key);
    public CompositeProjectionKey? ParentValue => Parent is null ? null : Convert(Parent);
    private static CompositeProjectionKey Convert(IProjectionKeyValues value)
    {
        var parts = new List<object?>();
        value.Append(parts);
        return new CompositeProjectionKey(parts);
    }
}
internal sealed class ProjectionPair<TParent, TItem>
{
    public TParent Parent { get; set; } = default!;
    public TItem Item { get; set; } = default!;
}
