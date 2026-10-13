using System.Linq.Expressions;

namespace Expresso.Rendering.EntityFrameworkCore;

internal sealed partial class EfCoreProjectionSourceValidator
{
    // Symbolic row definitions belong only to inspection; no executing query is rewritten.
    private readonly Dictionary<Expression, Expression> _rowDefinitions = new(ReferenceEqualityComparer.Instance);

    private Expression VisitLinqCall(MethodCallExpression node)
    {
        // Empty has no arguments; Range/Repeat start with scalar generator inputs, not a query source.
        if (node.Arguments.Count == 0 || !IsSequence(node.Arguments[0].Type)) return base.VisitMethodCall(node);
        Visit(node.Arguments[0]);
        var outer = RowDefinition(node.Arguments[0]);
        Expression? navigation = null;
        Expression? result = null;
        for (var i = 1; i < node.Arguments.Count; i++)
        {
            var argument = node.Arguments[i];
            var lambda = LambdaArgument(argument);
            if (lambda is null)
            {
                Visit(argument);
                continue;
            }
            var body = BindLambdaBody(node, i, lambda, outer, navigation);
            VisitStoredExpression(lambda, body);
            if (node.Method.Name == nameof(Queryable.SelectMany) && i == 1) navigation = body;
            if (node.Method.Name == nameof(Queryable.Select) ||
                (node.Method.Name is nameof(Queryable.Join) or nameof(Queryable.GroupJoin) && i == 4) ||
                (node.Method.Name == nameof(Queryable.SelectMany) && i == 2)) result = body;
        }
        if (IsSequence(node.Type))
        {
            if (result is not null) _rowDefinitions[node] = result;
            else if (navigation is not null) _rowDefinitions[node] = RowDefinition(navigation);
            else if (PreservesRowDefinition(node, outer)) _rowDefinitions[node] = outer;
            else if (node.Method.Name == nameof(Queryable.GroupBy) && SequenceElement(node.Type) is { IsGenericType: true } group &&
                group.GetGenericTypeDefinition() == typeof(IGrouping<,>)) _rowDefinitions[node] = Expression.Parameter(group, "group");
        }
        return node; // Only inspection uses bound expressions; the executing query is never rewritten.
    }

    private Expression BindLambdaBody(MethodCallExpression node, int argumentIndex, LambdaExpression lambda,
        Expression outer, Expression? navigation)
    {
        var values = new List<Expression> { outer };
        if (node.Method.Name is nameof(Queryable.Join) or nameof(Queryable.GroupJoin))
        {
            var inner = RowDefinition(node.Arguments[1]);
            // Join arguments: inner source (1), outer key (2), inner key (3), result selector (4).
            if (argumentIndex == 3) values[0] = inner;
            if (argumentIndex == 4)
                values.Add(node.Method.Name == nameof(Queryable.GroupJoin) ? node.Arguments[1] : inner);
        }
        // SelectMany's result selector (2) receives the collection selector's (1) item row.
        if (node.Method.Name == nameof(Queryable.SelectMany) && argumentIndex == 2)
            values.Add(RowDefinition(navigation!));
        return new Bindings(lambda.Parameters, values).Visit(lambda.Body)!;
    }

    private static bool PreservesRowDefinition(MethodCallExpression node, Expression outer)
    {
        // Widening casts retain the concrete row, including through later interface/object-typed filters.
        var type = SequenceElement(node.Type);
        return type is not null &&
            (type == outer.Type || type == SequenceElement(node.Arguments[0].Type) ||
             (node.Method.Name is nameof(Queryable.Cast) or nameof(Queryable.OfType) && type.IsAssignableFrom(outer.Type)));
    }

    private Expression RowDefinition(Expression source)
    {
        if (_rowDefinitions.TryGetValue(source, out var element)) return element;
        if (TryReadStoredValue(source, out var value) && value is IQueryable query &&
            _rowDefinitions.TryGetValue(query.Expression, out element)) return element;
        // An entity/group parameter is a symbolic query row, not a materialized value or application getter.
        var type = SequenceElement(source.Type) ?? typeof(object);
        if (IsSequence(type)) throw OpaqueSource(); // An unseen sequence-valued container element is not an inspectable query definition.
        return Expression.Parameter(type, "row");
    }

    private static Type? SequenceElement(Type type) => type.GetInterfaces().Prepend(type)
        .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))?.GetGenericArguments()[0];

    private sealed class Bindings : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, Expression> _values = new();
        internal Bindings(IReadOnlyList<ParameterExpression> parameters, IReadOnlyList<Expression> values)
        {
            for (var i = 0; i < Math.Min(parameters.Count, values.Count); i++)
                if (parameters[i].Type.IsAssignableFrom(values[i].Type)) _values.Add(parameters[i], values[i]);
        }
        protected override Expression VisitParameter(ParameterExpression node) => _values.TryGetValue(node, out var value) ? value : node;
        protected override Expression VisitMember(MemberExpression node)
        {
            var owner = Visit(node.Expression);
            return Member(node, owner);
        }
        private static Expression Member(MemberExpression node, Expression? owner)
        {
            if (owner is ConditionalExpression conditional)
                return Expression.Condition(conditional.Test, Member(node, conditional.IfTrue), Member(node, conditional.IfFalse), node.Type);
            if (owner is NewExpression { Members: not null } created)
            {
                var index = created.Members.IndexOf(node.Member);
                if (index >= 0) return created.Arguments[index];
            }
            if (owner is MemberInitExpression initialized)
            {
                var assignment = initialized.Bindings.OfType<MemberAssignment>().FirstOrDefault(b => b.Member == node.Member);
                if (assignment is not null) return assignment.Expression;
            }
            return node.Update(owner);
        }
    }
}
