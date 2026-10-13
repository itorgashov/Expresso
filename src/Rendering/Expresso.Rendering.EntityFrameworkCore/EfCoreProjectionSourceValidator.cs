using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections;

namespace Expresso.Rendering.EntityFrameworkCore;

internal sealed partial class EfCoreProjectionSourceValidator : ExpressionVisitor
{
    private readonly HashSet<IQueryable> _queries = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<IModel> _models = new(ReferenceEqualityComparer.Instance);

    public override Expression? Visit(Expression? node)
    {
        if (node is not null && IsOpaqueValue(node.Type) &&
            node is not (ConstantExpression or MemberExpression or UnaryExpression or MethodCallExpression or
                ConditionalExpression or QueryRootExpression or NewArrayExpression or BinaryExpression { NodeType: ExpressionType.Coalesce }) &&
            !(node is ParameterExpression && !typeof(IQueryable).IsAssignableFrom(node.Type) && !IsExpressionValue(node.Type)))
            throw OpaqueSource();
        return base.Visit(node);
    }

    internal static void Validate(IQueryable source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        new EfCoreProjectionSourceValidator().VisitQuery(source);
    }

    private void VisitQuery(IQueryable query)
    {
        // Captured queries can repeat or refer to themselves. Never use application equality or enumerate them.
        if (_queries.Add(query)) Visit(query.Expression);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        VisitStoredValue(node.Type, node.Value);
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (TryReadStoredValue(node, out var value))
            VisitStoredValue(node.Type, value);
        else if (IsExpressionValue(node.Type) || (IsSequence(node.Type) && !IsNavigation(node))) throw OpaqueSource();
        return base.VisitMember(node);
    }

    private void VisitStoredValue(Type declaredType, object? value)
    {
        // Inspect the actual value first: captures may be declared as object or IEnumerable rather than IQueryable.
        if (value is IQueryable query) VisitQuery(query);
        else if (value is Expression expression) VisitStoredExpression(expression);
        else if (IsExpressionValue(declaredType)) throw OpaqueSource();
        else if (IsSequence(declaredType) && !IsMaterialized(value)) throw OpaqueSource();
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (IsOpaqueValue(node.Type) && node.NodeType != ExpressionType.Quote)
        {
            if (node.Method is not null) throw OpaqueSource();
            if (!IsOpaqueValue(node.Operand.Type) &&
                (!TryReadStoredValue(node, out var value) ||
                 (value is not IQueryable && value is not Expression && !IsMaterialized(value))))
                throw OpaqueSource();
        }
        return base.VisitUnary(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (IsExpressionValue(node.Type)) throw OpaqueSource();
        if (node.Method.DeclaringType == typeof(Queryable) || node.Method.DeclaringType == typeof(Enumerable))
        {
            // First/Single/ElementAt/Aggregate can extract unseen query objects from accepted containers.
            // Composition returns a sequence type; extraction returns a generic value. Never evaluate the lookup.
            if (IsSequence(node.Type) && node.Method.IsGenericMethod &&
                node.Method.GetGenericMethodDefinition().ReturnType.IsGenericParameter) throw OpaqueSource();
            return VisitLinqCall(node);
        }
        if (IsSequence(node.Type) && !IsEfQueryWrapper(node) && !IsMappedPropertyCall(node))
            throw OpaqueSource();
        var visited = base.VisitMethodCall(node);
        if (IsSequence(node.Type) && node.Arguments.Count > 0 && IsEfQueryWrapper(node) &&
            SequenceElement(node.Type) == SequenceElement(node.Arguments[0].Type))
            _rowDefinitions[node] = RowDefinition(node.Arguments[0]);
        return visited;
    }

    private static bool IsEfQueryWrapper(MethodCallExpression node) =>
        node.Method.DeclaringType == typeof(EntityFrameworkQueryableExtensions) ||
        node.Method.DeclaringType == typeof(RelationalQueryableExtensions);

    private bool IsMappedPropertyCall(MethodCallExpression node) =>
        node.Method.DeclaringType == typeof(EF) && node.Method.Name == nameof(EF.Property) &&
        TryReadStoredValue(node.Arguments[1], out var name) && name is string property &&
        IsMappedAccess(node.Arguments[0], property);

    private static bool TryReadStoredValue(Expression expression, out object? value)
    {
        // Read fields only, including nested closure fields. Getters/factories/conversion operators may execute arbitrary code.
        if (expression is ConstantExpression constant) { value = constant.Value; return true; }
        if (expression is MemberExpression { Member: FieldInfo { IsStatic: false } field, Expression: { } owner } &&
            TryReadStoredValue(owner, out var target) && target is not null)
        { value = field.GetValue(target); return true; }
        if (expression is UnaryExpression { Method: null } unary &&
            unary.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs)
            return TryReadStoredValue(unary.Operand, out value);
        value = null;
        return false;
    }

    private static NotSupportedException OpaqueSource() => new("Cannot safely inspect a query source for collection projection. " +
        "Capture the IQueryable or expression in a variable instead of an opaque query-producing expression.");

    private static bool IsSequence(Type type) => type != typeof(string) && type != typeof(byte[]) && typeof(IEnumerable).IsAssignableFrom(type);

    private static bool IsMaterialized(object? value)
    {
        if (value is Array) return true;
        if (value is null) return false;
        var type = value.GetType();
        // BCL materialized containers are inspected by identity, not enumerated. Unknown/deferred captures remain opaque.
        return type.Assembly == typeof(List<>).Assembly &&
            (value is ICollection || type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>)));
    }

    private bool IsNavigation(MemberExpression node) => node.Expression is not null && IsMappedAccess(node.Expression, node.Member.Name);

    protected override Expression VisitExtension(Expression node)
    {
        if (node is EntityQueryRootExpression root)
        {
            _models.Add(root.EntityType.Model);
            // SQL Server is an optional host dependency. Check root ancestry, without invoking its internal APIs.
            for (var type = node.GetType(); type is not null; type = type.BaseType)
                if (type.FullName == "Microsoft.EntityFrameworkCore.SqlServer.Query.Internal.TemporalQueryRootExpression")
                    throw new NotSupportedException("Temporal collection projection is not supported. Use a scalar-only projection or load the historical graph explicitly.");
        }
        return node is QueryRootExpression ? node : base.VisitExtension(node);
    }
}
